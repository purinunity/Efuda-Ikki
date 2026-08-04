import json


def response(status_code, body):
    return {
        "statusCode": status_code,
        "headers": {
            "content-type": "application/json; charset=utf-8",
            "cache-control": "no-store",
            "x-content-type-options": "nosniff",
        },
        "body": json.dumps(body, ensure_ascii=False, separators=(",", ":")),
    }


def parse_json_body(event, max_bytes=32768):
    raw = event.get("body") or ""
    if len(raw.encode("utf-8")) > max_bytes:
        raise ValueError("request_too_large")
    try:
        value = json.loads(raw)
    except (TypeError, json.JSONDecodeError) as exc:
        raise ValueError("invalid_json") from exc
    if not isinstance(value, dict):
        raise ValueError("json_object_required")
    return value


def error(status_code, code):
    return response(status_code, {"error": code})

