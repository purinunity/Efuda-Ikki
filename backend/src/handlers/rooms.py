from common.auth_context import authenticated_user
from common.game_engine import initial_state, player_view
from common.http import error, parse_json_body, response
from common.room_store import (
    create_room,
    get_room,
    get_room_by_code,
    join_room,
    normalize_room_code,
)


def handler(event, _context):
    try:
        user_id = authenticated_user(event)
        route = event.get("requestContext", {}).get("routeKey", "")
        if route == "POST /rooms":
            room = create_room(user_id, initial_state(user_id))
            return response(201, player_view(room, user_id))

        if route == "POST /rooms/join":
            body = parse_json_body(event, max_bytes=2048)
            room_code = normalize_room_code(body.get("roomCode"))
            room = get_room_by_code(room_code)
            if not room:
                return error(404, "room_not_found")
            if room.get("hostUserId") == user_id:
                return response(200, player_view(room, user_id))
            room = join_room(room["roomId"], user_id)
            return response(200, player_view(room, user_id))

        if route == "GET /rooms/{roomId}":
            room_id = (event.get("pathParameters") or {}).get("roomId")
            room = get_room(room_id)
            if not room:
                return error(404, "room_not_found")
            return response(200, player_view(room, user_id))

        return error(404, "route_not_found")
    except ValueError as exc:
        code = str(exc)
        status = 401 if "token" in code or "claims" in code else 400
        return error(status, code)
    except Exception as exc:
        if exc.__class__.__name__ == "ConditionalCheckFailedException":
            return error(409, "room_already_joined")
        return error(500, "internal_error")

