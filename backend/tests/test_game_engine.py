import unittest

from common.game_engine import apply_command, initial_state


class GameEngineTests(unittest.TestCase):
    def setUp(self):
        self.room = {
            "roomId": "room",
            "roomCode": "ABCDEFGH23",
            "hostUserId": "host",
            "guestUserId": "guest",
            "state": initial_state("host"),
        }

    def test_both_players_ready_starts_match(self):
        self.room["state"] = apply_command(
            self.room, "host", {"type": "ready"}
        )
        state = apply_command(self.room, "guest", {"type": "ready"})
        self.assertEqual("playing", state["phase"])
        self.assertEqual("host", state["turnUserId"])

    def test_rejects_action_out_of_turn(self):
        self.room["state"] = {
            **initial_state("host"),
            "phase": "playing",
            "turnUserId": "host",
        }
        with self.assertRaisesRegex(ValueError, "not_your_turn"):
            apply_command(
                self.room,
                "guest",
                {"type": "confirm", "payload": {}},
            )


if __name__ == "__main__":
    unittest.main()
