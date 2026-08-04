# Room Match Protocol

## 共通認証

HTTPは次のヘッダーを付与します。

```http
Authorization: Bearer <accessToken>
Content-Type: application/json
```

WebSocket接続時にも同じ `Authorization` ヘッダーを付与します。URLのクエリへ
トークンを入れるとアクセスログへ残りやすいため使用しません。

## Steamログイン

```http
POST /auth/steam
Content-Type: application/json

{"ticket":"<Steam Web API ticketの16進文字列>"}
```

成功:

```json
{
  "accessToken": "<JWT>",
  "expiresAt": 1760000000,
  "userId": "76561198000000000",
  "steamId": "76561198000000000",
  "authMethod": "steam"
}
```

## 開発用ログイン

開発用認証を有効にした非本番スタックでのみ使用できます。

```http
POST /auth/dev
Content-Type: application/json
X-Efuda-Dev-Key: <Secrets Managerで生成されたキー>

{"userId":"local_player_1"}
```

成功:

```json
{
  "accessToken": "<JWT>",
  "expiresAt": 1760000000,
  "userId": "dev:local_player_1",
  "authMethod": "development"
}
```

`userId` は英数字、`_`、`-` の1〜32文字です。2クライアントで対戦を試す場合は、
`local_player_1` と `local_player_2` のように異なるIDを指定します。

## ルーム作成

```http
POST /rooms
Authorization: Bearer <JWT>

{}
```

成功:

```json
{
  "roomId": "c9b3a58d-36c2-4dd2-8c4c-3626313819be",
  "roomCode": "ABCD234567",
  "status": "waiting",
  "version": 0,
  "state": {
    "phase": "waiting",
    "round": 0
  },
  "youAreHost": true,
  "opponentJoined": false
}
```

## ルーム参加

```http
POST /rooms/join
Authorization: Bearer <JWT>
Content-Type: application/json

{"roomCode":"ABCD234567"}
```

## 状態取得

```http
GET /rooms/{roomId}
Authorization: Bearer <JWT>
```

ルームのメンバー以外には状態を返しません。

## WebSocket

WebSocketのルートはJSONの `action` で決まります。各操作にはランダムな
`requestId` と、最後に受信した `version` を付けます。

```json
{
  "action": "matchCommand",
  "roomId": "c9b3a58d-36c2-4dd2-8c4c-3626313819be",
  "requestId": "b225d946-4592-4280-8d21-42d843aa9b65",
  "expectedVersion": 3,
  "command": {
    "type": "confirm",
    "payload": {}
  }
}
```

通信確認用のコマンドは `ready`、`selectSpecial`、`discard`、`confirm`、`resign`
です。状態更新後、接続中の両プレイヤーへ次を送ります。

```json
{
  "type": "roomState",
  "room": {
    "roomId": "c9b3a58d-36c2-4dd2-8c4c-3626313819be",
    "roomCode": "ABCD234567",
    "status": "ready",
    "version": 4,
    "state": {},
    "youAreHost": false,
    "opponentJoined": true
  }
}
```

競合や入力不正時:

```json
{"type":"error","error":"state_conflict"}
```

`state_conflict` を受けたクライアントは `GET /rooms/{roomId}` で最新状態を取得し、
画面を同期します。同じ操作を再送する場合は新しい状態を確認してから行います。

## 本番ゲームロジックへの拡張

クライアントからは「選んだ札」「捨てる札」「決定」などの意思だけを送ります。
手札の配布、役判定、特殊札効果、HP減算、勝敗はサーバー側で計算します。
対戦相手へ返す `player_view` では、相手の手札や未公開情報を必ず除外してください。
