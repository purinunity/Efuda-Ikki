import json
import urllib.parse
import urllib.request


STEAM_AUTH_URL = (
    "https://partner.steam-api.com/"
    "ISteamUserAuth/AuthenticateUserTicket/v1/"
)


def authenticate_ticket(publisher_key, app_id, ticket, identity):
    query = urllib.parse.urlencode(
        {
            "key": publisher_key,
            "appid": str(app_id),
            "ticket": ticket,
            "identity": identity,
        }
    )
    request = urllib.request.Request(
        f"{STEAM_AUTH_URL}?{query}",
        headers={"accept": "application/json"},
        method="GET",
    )
    with urllib.request.urlopen(request, timeout=5) as result:
        payload = json.loads(result.read().decode("utf-8"))

    params = payload.get("response", {}).get("params", {})
    if params.get("result") != "OK":
        raise ValueError("steam_ticket_rejected")
    steam_id = params.get("steamid")
    if not isinstance(steam_id, str) or not steam_id.isdigit():
        raise ValueError("steam_identity_missing")
    return {
        "steamId": steam_id,
        "ownerSteamId": str(params.get("ownersteamid") or steam_id),
        "vacBanned": bool(params.get("vacbanned", False)),
        "publisherBanned": bool(params.get("publisherbanned", False)),
    }

