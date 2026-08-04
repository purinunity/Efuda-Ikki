import unittest

from common.dev_auth import authenticate_request


class DevAuthTests(unittest.TestCase):
    def setUp(self):
        self.key = "a" * 48

    def test_accepts_matching_header(self):
        user_id = authenticate_request(
            {"headers": {"X-Efuda-Dev-Key": self.key}},
            {"userId": "player_1"},
            self.key,
        )
        self.assertEqual("dev:player_1", user_id)

    def test_rejects_wrong_key(self):
        with self.assertRaisesRegex(PermissionError, "invalid_dev_key"):
            authenticate_request(
                {"headers": {"x-efuda-dev-key": "b" * 48}},
                {"userId": "player_1"},
                self.key,
            )

    def test_rejects_non_ascii_key_as_unauthorized(self):
        with self.assertRaisesRegex(PermissionError, "invalid_dev_key"):
            authenticate_request(
                {"headers": {"x-efuda-dev-key": "不正なキー"}},
                {"userId": "player_1"},
                self.key,
            )

    def test_rejects_invalid_user_id(self):
        with self.assertRaisesRegex(ValueError, "invalid_dev_user_id"):
            authenticate_request(
                {"headers": {"x-efuda-dev-key": self.key}},
                {"userId": "../player"},
                self.key,
            )


if __name__ == "__main__":
    unittest.main()
