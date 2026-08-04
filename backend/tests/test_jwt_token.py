import time
import unittest

from common import jwt_token


class JwtTokenTests(unittest.TestCase):
    def test_round_trip(self):
        now = int(time.time())
        payload = {
            "sub": "76561198000000000",
            "iss": "issuer",
            "aud": "audience",
            "iat": now,
            "exp": now + 60,
        }
        token = jwt_token.encode(payload, "test-secret")
        decoded = jwt_token.decode(
            token, "test-secret", "issuer", "audience", now=now
        )
        self.assertEqual(payload["sub"], decoded["sub"])

    def test_rejects_modified_token(self):
        now = int(time.time())
        token = jwt_token.encode(
            {
                "sub": "user",
                "iss": "issuer",
                "aud": "audience",
                "iat": now,
                "exp": now + 60,
            },
            "test-secret",
        )
        with self.assertRaises(ValueError):
            jwt_token.decode(
                token + "x",
                "test-secret",
                "issuer",
                "audience",
                now=now,
            )

    def test_rejects_expired_token(self):
        now = int(time.time())
        token = jwt_token.encode(
            {
                "sub": "user",
                "iss": "issuer",
                "aud": "audience",
                "iat": now - 120,
                "exp": now - 1,
            },
            "test-secret",
        )
        with self.assertRaisesRegex(ValueError, "token_expired"):
            jwt_token.decode(
                token,
                "test-secret",
                "issuer",
                "audience",
                now=now,
            )


if __name__ == "__main__":
    unittest.main()
