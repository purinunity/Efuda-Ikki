import os
import secrets
import string
import time
import uuid
from decimal import Decimal

import boto3
from boto3.dynamodb.conditions import Key


ROOM_CODE_ALPHABET = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"
_dynamodb = boto3.resource("dynamodb")
_rooms = _dynamodb.Table(os.environ["ROOMS_TABLE"])
_connections = _dynamodb.Table(os.environ["CONNECTIONS_TABLE"])


def _normalize(value):
    if isinstance(value, Decimal):
        return int(value)
    if isinstance(value, list):
        return [_normalize(item) for item in value]
    if isinstance(value, dict):
        return {key: _normalize(item) for key, item in value.items()}
    return value


def create_room(host_user_id, initial_state):
    now = int(time.time())
    ttl = now + int(os.environ.get("ROOM_TTL_SECONDS", "10800"))
    for _ in range(5):
        room_id = str(uuid.uuid4())
        room_code = "".join(
            secrets.choice(ROOM_CODE_ALPHABET) for _ in range(10)
        )
        if get_room_by_code(room_code) is not None:
            continue
        item = {
            "roomId": room_id,
            "roomCode": room_code,
            "hostUserId": host_user_id,
            "status": "waiting",
            "version": 0,
            "state": initial_state,
            "processedRequests": {},
            "createdAt": now,
            "updatedAt": now,
            "expiresAt": ttl,
        }
        try:
            _rooms.put_item(
                Item=item,
                ConditionExpression="attribute_not_exists(roomId)",
            )
            return item
        except _rooms.meta.client.exceptions.ConditionalCheckFailedException:
            continue
    raise RuntimeError("room_code_generation_failed")


def get_room(room_id):
    result = _rooms.get_item(
        Key={"roomId": room_id}, ConsistentRead=True
    )
    item = result.get("Item")
    return _normalize(item) if item else None


def get_room_by_code(room_code):
    result = _rooms.query(
        IndexName="roomCode-index",
        KeyConditionExpression=Key("roomCode").eq(room_code),
        Limit=1,
    )
    items = result.get("Items") or []
    return _normalize(items[0]) if items else None


def join_room(room_id, user_id):
    now = int(time.time())
    result = _rooms.update_item(
        Key={"roomId": room_id},
        UpdateExpression=(
            "SET guestUserId = :guest, #status = :ready, updatedAt = :now"
        ),
        ConditionExpression=(
            "attribute_exists(roomId) AND hostUserId <> :guest AND "
            "attribute_not_exists(guestUserId) AND #status = :waiting"
        ),
        ExpressionAttributeNames={"#status": "status"},
        ExpressionAttributeValues={
            ":guest": user_id,
            ":ready": "ready",
            ":waiting": "waiting",
            ":now": now,
        },
        ReturnValues="ALL_NEW",
    )
    return _normalize(result["Attributes"])


def update_state(room_id, expected_version, request_id, next_state):
    now = int(time.time())
    result = _rooms.update_item(
        Key={"roomId": room_id},
        UpdateExpression=(
            "SET #state = :state, #version = :nextVersion, "
            "processedRequests.#requestId = :now, updatedAt = :now"
        ),
        ConditionExpression=(
            "#version = :expectedVersion AND "
            "attribute_not_exists(processedRequests.#requestId)"
        ),
        ExpressionAttributeNames={
            "#state": "state",
            "#version": "version",
            "#requestId": request_id,
        },
        ExpressionAttributeValues={
            ":state": next_state,
            ":nextVersion": expected_version + 1,
            ":expectedVersion": expected_version,
            ":now": now,
        },
        ReturnValues="ALL_NEW",
    )
    return _normalize(result["Attributes"])


def put_connection(connection_id, user_id):
    now = int(time.time())
    _connections.put_item(
        Item={
            "connectionId": connection_id,
            "userId": user_id,
            "connectedAt": now,
            "expiresAt": now + 3 * 60 * 60,
        }
    )


def get_connection(connection_id):
    result = _connections.get_item(Key={"connectionId": connection_id})
    return _normalize(result.get("Item")) if result.get("Item") else None


def delete_connection(connection_id):
    _connections.delete_item(Key={"connectionId": connection_id})


def connections_for_user(user_id):
    result = _connections.query(
        IndexName="userId-index",
        KeyConditionExpression=Key("userId").eq(user_id),
    )
    return [
        item["connectionId"] for item in result.get("Items", [])
    ]


def normalize_room_code(value):
    normalized = str(value or "").strip().upper()
    if (
        len(normalized) != 10
        or any(char not in ROOM_CODE_ALPHABET for char in normalized)
    ):
        raise ValueError("invalid_room_code")
    return normalized


def valid_request_id(value):
    value = str(value or "")
    allowed = string.ascii_letters + string.digits + "-_"
    if not 8 <= len(value) <= 64 or any(char not in allowed for char in value):
        raise ValueError("invalid_request_id")
    return value
