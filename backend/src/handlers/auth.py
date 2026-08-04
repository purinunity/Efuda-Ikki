import os
import re
import time
import urllib.error

from common import jwt_token
from common.dev_auth import authenticate_request
from common.http import error, parse_json_body, response
from common.secrets import get_json_secret, get_secret, jwt_secret
from common.steam import authenticate_ticket


TICKET_PATTERN = re.compile(r"^[0-9a-fA-F]{32,8192}$")


def handler(event, _context):
    route = event.get("requestContext", {}).get("routeKey", "")
    if route == "POST /auth/dev":
        return _dev_login(event)
    if route != "POST /auth/steam":
        return error(404, "route_not_found")

    try:
        body = parse_json_body(event)
        ticket = str(body.get("ticket") or "")
        if not TICKET_PATTERN.fullmatch(ticket):
            return error(400, "invalid_steam_ticket_format")
    except ValueError as exc:
        return error(400, str(exc))

    try:
        steam_secret = get_json_secret(os.environ["STEAM_SECRET_ARN"])
        publisher_key = steam_secret.get("publisherApiKey")
        if not publisher_key or publisher_key == "REPLACE_ME":
            return error(503, "steam_auth_not_configured")

        identity = authenticate_ticket(
            publisher_key,
            int(os.environ["STEAM_APP_ID"]),
            ticket,
            os.environ["STEAM_TICKET_IDENTITY"],
        )
        if identity["publisherBanned"]:
            return error(403, "publisher_banned")

        return _issue_token(
            identity["steamId"],
            "steam",
            owner_user_id=identity["ownerSteamId"],
        )
    except ValueError as exc:
        return error(401, str(exc))
    except (urllib.error.URLError, TimeoutError):
        return error(503, "steam_auth_unavailable")
    except Exception:
        return error(500, "internal_error")


def _dev_login(event):
    if os.environ.get("ENABLE_DEV_AUTH", "false").lower() != "true":
        return error(404, "route_not_found")

    try:
        body = parse_json_body(event, max_bytes=2048)
        secret_arn = os.environ.get("DEV_AUTH_SECRET_ARN", "")
        user_id = authenticate_request(event, body, get_secret(secret_arn))
        return _issue_token(user_id, "development")
    except PermissionError as exc:
        return error(401, str(exc))
    except ValueError as exc:
        return error(400, str(exc))
    except Exception:
        return error(500, "internal_error")


def _issue_token(user_id, auth_method, owner_user_id=None):
    now = int(time.time())
    expires_at = now + int(os.environ.get("JWT_TTL_SECONDS", "900"))
    claims = {
        "sub": user_id,
        "iss": os.environ["JWT_ISSUER"],
        "aud": os.environ["JWT_AUDIENCE"],
        "iat": now,
        "exp": expires_at,
        "auth": auth_method,
        "permissions": ["room:create", "room:join", "match:play"],
    }
    if owner_user_id:
        claims["owner"] = owner_user_id
    token = jwt_token.encode(claims, jwt_secret())
    body = {
        "accessToken": token,
        "expiresAt": expires_at,
        "userId": user_id,
        "authMethod": auth_method,
    }
    if auth_method == "steam":
        body["steamId"] = user_id
    return response(
        200,
        body,
    )
