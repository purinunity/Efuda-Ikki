import os

from common import jwt_token
from common.secrets import jwt_secret


def authenticated_user(event):
    token = jwt_token.bearer_token(event)
    claims = jwt_token.decode(
        token,
        jwt_secret(),
        os.environ["JWT_ISSUER"],
        os.environ["JWT_AUDIENCE"],
    )
    return claims["sub"]

