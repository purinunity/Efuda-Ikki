import base64
import binascii
import hashlib
import hmac
import json
import time


def _b64url_encode(value):
    return base64.urlsafe_b64encode(value).rstrip(b"=").decode("ascii")


def _b64url_decode(value):
    padding = "=" * (-len(value) % 4)
    return base64.urlsafe_b64decode(value + padding)


def encode(payload, secret):
    header = {"alg": "HS256", "typ": "JWT"}
    parts = [
        _b64url_encode(json.dumps(header, separators=(",", ":")).encode()),
        _b64url_encode(json.dumps(payload, separators=(",", ":")).encode()),
    ]
    signing_input = ".".join(parts).encode("ascii")
    signature = hmac.new(secret.encode(), signing_input, hashlib.sha256).digest()
    return ".".join(parts + [_b64url_encode(signature)])


def decode(token, secret, issuer, audience, now=None):
    try:
        encoded_header, encoded_payload, encoded_signature = token.split(".")
        signing_input = f"{encoded_header}.{encoded_payload}".encode("ascii")
        actual = _b64url_decode(encoded_signature)
        expected = hmac.new(
            secret.encode(), signing_input, hashlib.sha256
        ).digest()
        if not hmac.compare_digest(actual, expected):
            raise ValueError("invalid_signature")
        header = json.loads(_b64url_decode(encoded_header))
        payload = json.loads(_b64url_decode(encoded_payload))
    except (
        ValueError,
        TypeError,
        binascii.Error,
        UnicodeDecodeError,
        json.JSONDecodeError,
    ) as exc:
        raise ValueError("invalid_token") from exc

    if not isinstance(header, dict) or not isinstance(payload, dict):
        raise ValueError("invalid_token")
    current_time = int(time.time() if now is None else now)
    if header.get("alg") != "HS256":
        raise ValueError("invalid_algorithm")
    if payload.get("iss") != issuer or payload.get("aud") != audience:
        raise ValueError("invalid_claims")
    if not isinstance(payload.get("sub"), str) or not payload["sub"]:
        raise ValueError("invalid_subject")
    if int(payload.get("exp", 0)) <= current_time:
        raise ValueError("token_expired")
    if int(payload.get("iat", current_time + 1)) > current_time + 30:
        raise ValueError("invalid_issued_at")
    return payload


def bearer_token(event):
    headers = {
        str(key).lower(): value
        for key, value in (event.get("headers") or {}).items()
    }
    authorization = headers.get("authorization", "")
    if not authorization.startswith("Bearer "):
        raise ValueError("missing_bearer_token")
    return authorization[7:].strip()
