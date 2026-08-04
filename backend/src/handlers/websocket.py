import json
import os

import boto3
from botocore.exceptions import ClientError

from common.auth_context import authenticated_user
from common.game_engine import apply_command, player_view
from common.room_store import (
    connections_for_user,
    delete_connection,
    get_connection,
    get_room,
    put_connection,
    update_state,
    valid_request_id,
)


def handler(event, _context):
    request_context = event.get("requestContext") or {}
    route = request_context.get("routeKey")
    connection_id = request_context.get("connectionId")

    try:
        if route == "$connect":
            user_id = authenticated_user(event)
            put_connection(connection_id, user_id)
            return {"statusCode": 200}

        if route == "$disconnect":
            if connection_id:
                delete_connection(connection_id)
            return {"statusCode": 200}

        connection = get_connection(connection_id)
        if not connection:
            return {"statusCode": 401, "body": "unauthorized"}
        user_id = connection["userId"]

        body = _parse_message(event)
        if body.get("action") != "matchCommand":
            _send(connection_id, {"type": "error", "error": "unsupported_action"})
            return {"statusCode": 200}

        room_id = str(body.get("roomId") or "")
        request_id = valid_request_id(body.get("requestId"))
        expected_version = body.get("expectedVersion")
        command = body.get("command")
        if (
            not room_id
            or not isinstance(expected_version, int)
            or not isinstance(command, dict)
        ):
            raise ValueError("invalid_match_command")

        room = get_room(room_id)
        if not room:
            raise ValueError("room_not_found")
        next_state = apply_command(room, user_id, command)
        updated = update_state(
            room_id, expected_version, request_id, next_state
        )
        _broadcast_room(updated)
        return {"statusCode": 200}
    except ValueError as exc:
        if route == "$connect":
            return {"statusCode": 401, "body": "unauthorized"}
        if connection_id:
            _send(connection_id, {"type": "error", "error": str(exc)})
        return {"statusCode": 200}
    except Exception as exc:
        if route == "$connect":
            return {"statusCode": 500, "body": "connection_failed"}
        code = (
            "state_conflict"
            if exc.__class__.__name__ == "ConditionalCheckFailedException"
            else "internal_error"
        )
        if connection_id:
            _send(connection_id, {"type": "error", "error": code})
        return {"statusCode": 200}


def _parse_message(event):
    raw = event.get("body") or ""
    if len(raw.encode("utf-8")) > 16384:
        raise ValueError("message_too_large")
    try:
        body = json.loads(raw)
    except json.JSONDecodeError as exc:
        raise ValueError("invalid_json") from exc
    if not isinstance(body, dict):
        raise ValueError("json_object_required")
    return body


def _broadcast_room(room):
    for user_id in {room.get("hostUserId"), room.get("guestUserId")}:
        if not user_id:
            continue
        view = player_view(room, user_id)
        message = {"type": "roomState", "room": view}
        for connection_id in connections_for_user(user_id):
            _send(connection_id, message)


def _send(connection_id, payload):
    domain = os.environ["WEBSOCKET_API_ID"]
    stage = os.environ["WEBSOCKET_STAGE"]
    endpoint = (
        f"https://{domain}.execute-api."
        f"{os.environ['AWS_REGION']}.amazonaws.com/{stage}"
    )
    client = boto3.client(
        "apigatewaymanagementapi", endpoint_url=endpoint
    )
    try:
        client.post_to_connection(
            ConnectionId=connection_id,
            Data=json.dumps(payload, separators=(",", ":")).encode(),
        )
    except ClientError as exc:
        if exc.response.get("ResponseMetadata", {}).get("HTTPStatusCode") == 410:
            delete_connection(connection_id)
            return
        raise
