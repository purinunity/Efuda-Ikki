ALLOWED_COMMANDS = {"ready", "selectSpecial", "discard", "confirm", "resign"}


def initial_state(host_user_id):
    return {
        "phase": "waiting",
        "round": 0,
        "turnUserId": None,
        "readyUserIds": [],
        "lastAction": None,
        "winnerUserId": None,
        "hostUserId": host_user_id,
    }


def apply_command(room, user_id, command):
    if user_id not in {
        room.get("hostUserId"),
        room.get("guestUserId"),
    }:
        raise ValueError("not_room_member")

    command_type = command.get("type")
    if command_type not in ALLOWED_COMMANDS:
        raise ValueError("unsupported_command")

    state = dict(room.get("state") or {})
    if state.get("winnerUserId"):
        raise ValueError("match_already_finished")

    if command_type == "ready":
        ready = list(state.get("readyUserIds") or [])
        if user_id not in ready:
            ready.append(user_id)
        state["readyUserIds"] = ready
        if room.get("guestUserId") and len(ready) == 2:
            state["phase"] = "playing"
            state["round"] = 1
            state["turnUserId"] = room["hostUserId"]
        return state

    if state.get("phase") != "playing":
        raise ValueError("match_not_playing")

    if command_type == "resign":
        state["winnerUserId"] = opponent_user_id(room, user_id)
        state["phase"] = "finished"
        state["lastAction"] = {"userId": user_id, "type": "resign"}
        return state

    if state.get("turnUserId") != user_id:
        raise ValueError("not_your_turn")

    payload = command.get("payload")
    if payload is not None and not isinstance(payload, dict):
        raise ValueError("invalid_command_payload")

    state["lastAction"] = {
        "userId": user_id,
        "type": command_type,
        "payload": payload or {},
    }
    state["turnUserId"] = opponent_user_id(room, user_id)
    return state


def opponent_user_id(room, user_id):
    if user_id == room.get("hostUserId"):
        return room.get("guestUserId")
    return room.get("hostUserId")


def player_view(room, user_id):
    if user_id not in {
        room.get("hostUserId"),
        room.get("guestUserId"),
    }:
        raise ValueError("not_room_member")
    return {
        "roomId": room["roomId"],
        "roomCode": room["roomCode"],
        "status": room["status"],
        "version": room["version"],
        "state": room["state"],
        "youAreHost": user_id == room.get("hostUserId"),
        "opponentJoined": bool(room.get("guestUserId")),
    }

