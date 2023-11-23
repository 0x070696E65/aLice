# - aLice -

\- aLice \-はSymbolアカウントで署名ができるMobileAppです。
ユーザーはSymbolアカウントの秘密鍵をユーザが設定するパスワードにて暗号化し端末に保存しています。

他のMobileAppやwebサイトから署名が必要なデータを送ることでユーザが署名し署名データを返すことができます。

\- aLice \- が出来ることは以下です
1. 公開鍵を渡す
2. 署名する
    - Symbolトランザクションへの署名
    - UTF-8文字列への署名
    - バイナリデータへの署名（要16進数文字列化）
    - 複数のInnerTransactionに署名（Metal用）

### カスタムURLスキーム

\- aLice \- のカスタムURLスキームは `alice://` です。prefixには`sign`を使います。

### クエリパラメータ
| クエリパラメータ       | 値                                                                                                          | 
|----------------|------------------------------------------------------------------------------------------------------------| 
| type           | request_sign_transaction, request_sign_utf8, request_sign_binary_hex, request_pubkey, request_sign_batches |
| data           | 署名するデータ(string)                                                                                            |
| callback       | これらをhexにする（https://hogehoge.com/callback, sampleApp://callback/）                                           |
| batch          | batch0, batch1, batch2 ...                                                                                 |
| method         | get, post, announce                                                                                        |
| redirect_url   | これらをhexにする (https://google.com)                                                                            |
| set_public_key | 公開鍵を渡してアカウントを指定する                                                                                          |
| deadline       | トランザクションのDeadlineをaLiceで設定する                                                                               |
| node           | request_sign_transactionのみノードを指定することでaLiceがアナウンスします（method=announce)                                       |
<br>
ex)
`alice://sign?data=${data}&type=${request_type}&callback=${callbackURL}`

### POST通信
`&method=post`を与えてやるとPOSTにて送信します。
その際は、aLiceの画面に留まるため
`&redirect_url=${redirect_url}`を追加してやると画面が遷移します。`redirect_url`にパラメータを追加するとそのまま遷移するので`token`などの受け渡しに使えるかと思います。
※redirect_urlもutf8をhexにして渡してください

postの場合は以下のようなJSONをBodyに含め送信します
```javascript
{
	"pubkey": "PUBLIC_KEY",
	"original_data": "ORIGINAL_DATA",
	"signature": "SIGNATURE"
}
```

### callbackUrl
ここに設定したURLに遷移（もしくはPOST送信）します。
ただし、これらは16進数にエンコードしてください
```javascript
const callback = `sample://callback/param1=test`;
const url = `alice://sign?data=${transferTransaction.serialize()}&type=request_sign_transaction&callback=${Convert.utf8ToHex(callback)}`;
```

callbackUrlを渡さない場合は、aLice内で署名データを表示しコピーさせることができます。
webの場合はこのほうがいいかもしれません。

#### request_sign_transaction
Symbolのトランザクションペイロードに署名します。
```javascript
const transaction = facade.transactionFactory.create({
    type: 'transfer_transaction_v1',
    signerPublicKey: aliceKeyPair.publicKey,
    recipientAddress: 'BOB_PLAIN_ADDRESS',
    mosaics: [
		{ 
            mosaicId: 0x72C0212E67A08BCEn,
            amount: 1_000000n 
        }
	],
    message: [0,...(new TextEncoder('utf-8')).encode('Hello, Symbol!!')],
    fee: 1_000000n,
    deadline,
});
const payload = symbolSdk.utils.uint8ToHex(transaction.serialize());
```
`alice://sign?type=request_sign_transaction&data=${payload}&callback=${callbackURL}`

sdk v2系の場合はhexへのコンバートは不要です
```javascript
// v2
const payload = transaction.serialize();
```

> Note: トランザクションのSignerPublicKeyはユーザが設定しているメインアカウントの公開鍵が自動的に設定されます。

以下をCallbackURLに返します。

`{callbackUrl}?signed_payload=${signedPayload}&pubkey=${publicKey}&original_data=${data}`

signedPayloadは以下のように扱います
```javascript
// v2 v2系では一度再構築しなおす必要があります
const hash = Transaction.createTransactionHash(payload, [... Convert.hexToUint8(generationHash)])
const signed = TransactionMapping.createFromPayload(payload);
const signedTx = new SignedTransaction(payload, hash, signed.signer?.publicKey!, signed.type, signed.networkType);

// v3 v3系ではそのままpayloadとして扱えますがput送信する時には以下のように扱う
const jsonObject = {
    payload: payload
};
const d = JSON.stringify(jsonObject);
```
> Note: ここでのoriginal_dataは送信時のhex_payloadです。

#### deadline
`&deadline=3600`を与えてやるとaLiceでDeadlineを設定できます。
単位は秒です

#### Announce
request_sign_transactionのみ`&method=announce`を与えてやるとaLiceにてアナウンスします。
nodeのurlは16進数に変換してください。

```javascript
const node = `https://samplenode.com:3001`;
const url = `alice://sign?data=${transferTransaction.serialize()}&type=request_sign_transaction&node=${Convert.utf8ToHex(node)}&method=announce&deadline=3600`;
```

#### 暗号化メッセージ
暗号化メッセージはTransferTransactionのみで使えます。
パラメータにに`&recipient_publicKey_for_encrypt_message=${RecipientPublicKey}`を追加で渡してください。なお、暗号化することでトランザクションのサイズは大きくなり手数料が変わります。追加で`&fee_multiplier=100`とすることで手数料乗数を設定できます。このパラメータがない場合は100に設定されます・

v2の場合はPlainMessageで送信してください、またクエリに`sdk_version=v2`を追加してください。
```js
const tx = TransferTransaction.create(
  Deadline.create(1667250467),
  bob.address,
  [],
  PlainMessage.create('test'),
  NetworkType.TEST_NET
);
// URL例
`alice://sign?data=${tx.serialize()}&type=request_sign_transaction&recipient_publicKey_for_encrypt_message=${bob.publicKey}&sdk_version=v2`;
```

v3の場合はMessage領域にUTF8をbyte[]に変換し冒頭に[0x01]を追加してください

#### 注意点
トランザクションやインナートランザクションに公開鍵が設定されていない場合、aLiceのメインアカウントを自動的に設定します。
インナートランザクションは注意書きが表示されますが意図していないトランザクションにならないよう注意して作成してください。
またユーザーへの注意喚起なども合わせてご協力願います。

#### request_sign_utf8
UTF8文字列に署名します
文字列はそのままではなく16進数文字列にしてから送信してください
```js
const data = Buffer.from('hello,symbol!', 'utf-8').toString('hex').toUpperCase()
```
`alice://sign?type=request_sign_utf8&data={data}&callback={callbackURL}`

以下をCallbackURLに返します。
`{callbackUrl}?signature=${signature}&pubkey=${publicKey}&original_data=${data}`

#### request_sign_binary_hex
バイナリデータに署名します
```javascript
// v3
const data = symbolSdk.default.utils.uint8ToHex(new Uint8Array([0x00, 0x00, 0x00, 0x00, 0x00, 0x00]))
// v2
const data = Convert.uint8ToHex(new Uint8Array([0x00, 0x00, 0x00, 0x00, 0x00, 0x00]))
```
`alice://sign?type=request_sign_binary_hex&data=${data}&callback=${callbackURL}`

以下をCallbackURLに返します。
`{callbackUrl}?signature=${signature}&pubkey=${publicKey}&original_data=${data}`

#### request_sign_batches
複数のInnerTransactionに全て署名します。
今の所Metal用です。
以下のようにtxsの16進数文字列をbatchクエリに追加してください。
データ数が大きくなることが想定されます。`&method=post`にしてください。URLが長すぎると画面遷移できません。
```javascript
import {MetalService} from "metal-on-symbol";
const { txs, key, additive } = await metalService.createForgeTxs(
    type, 
    sourcePubAccount,
    targetPubAccount,
    targetId,
    payaload,
    additive,
    metadataPool
); 
let url = `alice://sign?type=request_sign_batches&method=post&callback=${callbackURL}`;
for(let i = 0; i < txs.length; i++){
  url += `&batch${i}=${tx.serialize()}`
}
```
以下のようなJSONをBodyに含め送信します

```javascript
{
	"pubkey": "PUBLIC_KEY",
	"signed0": "SIGNED_PAYLOAD_0",
	"signed1": "SIGNED_PAYLOAD_1",
	"signed2": "SIGNED_PAYLOAD_2",
	...
}
```

#### request_pubkey
ユーザが \- aLice \- のメインに設定しているアカウントの公開鍵(string)を返します。

#### set_public_key
このクエリに公開鍵を渡すとそのアカウントがaLiceに登録されている場合に、指定して署名リクエストができます。<br>
もしくは、トランザクションのSignerPublicKeyを指定することで、そのアカウントで署名できます。
指定した場合、ユーザーがaLice内にそのアカウントを保有していない場合はエラーとなります。
保有していてもアカウントの変更に応じない場合も同じくエラーとなります。

### エラーについて
ユーザが署名を拒否すると以下を返します

`{callback}?error=sign_rejected`

### 署名の検証について
#### トランザクションの検証
v3系の場合
```javascript
// 返ってきた公開鍵とトランザクションの公開鍵が一致するかどうか
const publicKey = new symbolSdk.default.PublicKey("PUBLIC_KEY");
const tx = symbolSdk.default.symbol.TransactionFactory.deserialize(symbolSdk.utils.hexToUint8(payload));
console.log(symbolSdk.default.utils.uint8ToHex(tx.signerPublicKey.bytes) == symbolSdk.utils.uint8ToHex(publicKey.bytes));

// トランザクションの検証
console.log(facade.verifyTransaction(tx, tx.signature));
```
#### その他署名の検証
v3系の場合
```javascript
const publicKey = new symbolSdk.default.PublicKey("PUBLIC_KEY");
const verifier = new facade.constructor.Verifier(publicKey);
console.log(verifier.verify(original_data, signature));
```

