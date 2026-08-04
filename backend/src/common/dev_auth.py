import hmac
import re


USER_ID_PATTERN = re.compile(r"^[A-Za-z0-9_-]{1,32}$")


def authenticate_request(event, body, expected_key):
    if not expected_key or len(expected_key) < 32:
        raise RuntimeError("dev_auth_not_configured")

    headers = {
        str(key).lower(): str(value)
        for key, value in (event.get("headers") or {}).items()
    }
    provided_key = headers.get("x-efuda-dev-key", "")
    if not hmac.compare_digest(
        provided_key.encode("utf-8"),
        expected_key.encode("utf-8"),
    ):
        raise PermissionError("invalid_dev_key")

    user_id = str(body.get("userId") or "")
    if not USER_ID_PATTERN.fullmatch(user_id):
        raise ValueError("invalid_dev_user_id")
    return f"dev:{user_id}"
