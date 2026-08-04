import json
import os

import boto3


_client = boto3.client("secretsmanager")
_cache = {}


def get_secret(arn):
    if arn in _cache:
        return _cache[arn]
    result = _client.get_secret_value(SecretId=arn)
    value = result.get("SecretString")
    if not value:
        raise RuntimeError("binary_secrets_are_not_supported")
    _cache[arn] = value
    return value


def get_json_secret(arn):
    value = json.loads(get_secret(arn))
    if not isinstance(value, dict):
        raise RuntimeError("secret_must_be_json_object")
    return value


def jwt_secret():
    return get_secret(os.environ["JWT_SECRET_ARN"])

