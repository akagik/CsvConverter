# CSV Converter

CSV から class を生成して、ScriptableObject に変換する.

# Requirements
Generic の CsvParser が必要:
git@github.com:akagik/Generic.git

## How to use

1. Project View で右クリックし Create/CsvConverterSettings を選択する.
2. 作成された設定ファイルに csv の File Path や 生成先フォルダ destination など各種変数を埋める.
3. Tools/CsvConvert/Generate Code を実行してクラス（テーブルクラス）を生成する.
4. Tools/CsvConvert/Create Assets を実行して csv ファイルパスから ScriptableObject を生成する.

## 各種設定項目

| 項目 | 説明 |
| ------------- | ------------- |
| File Path | csv までのファイルパス(Assets は除く)  |
| Class Name | Scripable Object のクラス名 |
| Destination | 生成した ScriptableObject を配置するディレクトリへのパス |
| Class Generate | クラスを生成するかどうか. |
| Table Generate | テーブルクラスとScriptableObjectを生成するかどうか. |
| Key | ScriptableObject の名前をつけるときに利用する一意な値を持つフィールド名 |
