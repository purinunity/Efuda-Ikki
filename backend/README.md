# Efuda Ikki Room Match Backend

Steam認証済みの2人がルームコードで合流し、WebSocketで対戦操作を交換するための
AWS SAM構成です。AWS側のコードと設定は、このディレクトリ内だけで完結します。

現段階では、認証・ルーム・接続・排他更新を試せるバックエンドの土台です。
`src/common/game_engine.py` の対戦処理は通信確認用の最小実装であり、絵札一揆の
役判定や特殊札処理をまだ移植していません。本番では勝敗をUnityから送信せず、
同ファイルへサーバー用ゲームロジックを実装してください。

## 構成

```text
Unity
  | Steam Web API ticket
  v
API Gateway HTTP API -> Auth Lambda -> Steamworks Web API
  | short-lived JWT
  +-> Rooms Lambda -> DynamoDB Rooms
  |
  +-> API Gateway WebSocket -> WebSocket Lambda -> DynamoDB

Secrets Manager
  - Steam Publisher Web API Key
  - JWT署名鍵
```

- `POST /auth/steam`: Steam認証チケットを検証し、15分有効のJWTを返す
- `POST /auth/dev`: 開発環境だけで専用キーを検証し、テスト用JWTを返す
- `POST /rooms`: 10文字のルームコードを発行
- `POST /rooms/join`: ルームコードで参加
- `GET /rooms/{roomId}`: 再接続時などに現在状態を取得
- WebSocket: 対戦操作を受信し、両プレイヤーへ状態を配信
- DynamoDB: 状態バージョンとリクエストIDで競合・二重操作を拒否
- TTL: ルームと接続情報を3時間後に自動削除

## セキュリティと費用

- Steam Publisher Web API KeyとJWT署名鍵はSecrets Managerにのみ保存します。
- UnityにはSteamの短命な認証チケットと、サーバー発行の短命JWTだけを持たせます。
- HTTP/WebSocket APIを毎秒10リクエスト、バースト20に制限しています。
- Lambdaの予約同時実行数は認証5、ルーム10、WebSocket 15です。
- DynamoDBオンデマンド容量にも読み取り100、書き込み50の上限があります。
- 任意で月額予算の80%到達時と100%予測時に通知します。
- CloudWatch Logsは7日で削除されます。チケットや秘密鍵はログ出力しません。

AWS Budgetsは通知機能で、課金を自動停止する機能ではありません。上記のスロットル、
同時実行数、DynamoDB上限が異常アクセス時の実消費を抑える役割を持ちます。
認証API自体はSteamチケットを受け取るため公開エンドポイントですが、ゲームAPIは
Steam検証後のJWTがなければ利用できません。

## 必要なもの

- SteamworksのApp ID
- Steamworks Web API Publisher Key
- AWS CLI
- AWS SAM CLI
- Python 3.13（ローカルテスト時）
- デプロイ先AWSアカウントの認証情報

## デプロイ

1. 設定例をコピーします。

   ```powershell
   Copy-Item samconfig.toml.example samconfig.toml
   ```

2. `samconfig.toml` の `SteamAppId`、リージョン、通知メールを変更します。
   メール通知が不要なら `BudgetAlertEmail=` としてください。

3. 検証、ビルド、デプロイを実行します。

   ```powershell
   sam validate --lint
   sam build
   sam deploy
   ```

4. CloudFormation出力 `SteamPublisherSecretArn` のシークレットをAWSコンソールで開き、
   `publisherApiKey` の `REPLACE_ME` をSteamworksのPublisher Keyに置き換えます。
   `deploymentNonce` はそのままで構いません。

5. CloudFormation出力の `HttpApiUrl` と `WebSocketUrl` をUnityの環境別設定へ登録します。
   URLやApp IDは公開情報ですが、Publisher KeyとJWT署名鍵は登録しないでください。

削除時は次を実行します。

```powershell
sam delete --stack-name efuda-room-match-dev
```

## Steam認証フロー

1. UnityでSteamworks SDKの `GetAuthTicketForWebApi("efuda-room-match")` を呼びます。
2. 得られたチケットを16進文字列にして `POST /auth/steam` へ送ります。
3. サーバーはSteamworks Web APIでチケット、App ID、Identityを検証します。
4. 返却された `accessToken` をHTTPとWebSocket接続に使用します。
5. 期限切れ時は新しいSteamチケットを取得し直して再認証します。

Steamworks側で設定するIdentityと、SAMパラメーター `SteamTicketIdentity` は完全に
一致させてください。

## 開発用認証

`EnvironmentName=dev EnableDevAuth=true` でデプロイした場合だけ、
`POST /auth/dev` が有効になります。専用キーはSecrets Managerで自動生成され、
CloudFormation出力 `DevAuthSecretArn` から参照できます。

```powershell
$stack = "efuda-room-match-dev"
$secretArn = aws cloudformation describe-stacks `
  --stack-name $stack `
  --query "Stacks[0].Outputs[?OutputKey=='DevAuthSecretArn'].OutputValue" `
  --output text
$env:EFUDA_DEV_AUTH_KEY = aws secretsmanager get-secret-value `
  --secret-id $secretArn `
  --query SecretString `
  --output text
```

キーはローカル環境変数などで保持し、Unityプロジェクト、設定ファイル、ログ、
配布ビルドへ保存しないでください。`EnvironmentName=prod` では
`EnableDevAuth=true` のデプロイをCloudFormationが拒否します。無効時の
`/auth/dev` は `404` を返します。

リクエスト形式は [PROTOCOL.md](PROTOCOL.md) を参照してください。

## ローカルテスト

外部パッケージは使っていません。`src` をモジュール探索先に追加して実行します。

```powershell
$env:PYTHONPATH = "$PWD\src"
python -m unittest discover -s tests -v
```

APIのメッセージ形式は [PROTOCOL.md](PROTOCOL.md) を参照してください。
