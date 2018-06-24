# CSV Converter

CSV から class を生成して、ScriptableObject に変換する.

# Requirements
Generic の CsvParser が必要:
`git@github.com:akagik/Generic.git`

## How to use

1. Project View で右クリックし Create/CsvConverterSettings を選択する.
2. 作成された設定ファイルに csv の File Path や 生成先フォルダ destination など各種変数を埋める.
3. Window/CsvConverter でウィンドウを開く.
3. Generate Code ボタンを押してクラス（テーブルクラス）を生成する.
4. Create Assets を実行して ScriptableObject を生成する.

## 対応している型
| 型 | 例 | 備考 |
| --- | ------------- | ----------- |
| int | 21 | |
| string | "hello world" | 実際の csv にはダブルクォーテーションを入力する必要はない. |
| float | 1.3 | |
| double | 2.5 | |
| long | 9032 | |
| bool | true | |
| Sprite | "test.png" | (Assets からの)フルパスで見つからない場合はファイル名で検索する. |
| Vector2 | (4.5, 9.1) | |
| Vector3 | (1.0, 2.2, 3.4) | |
| ユーザー定義Enum | 変数名 (Red など文字列を入力する) | 整数値は無効. |
| 上記型の配列 | ["abc", "def"] | 空の場合は空配列になる. |


## 各種設定項目

| 項目 | 説明 |
| ------------- | ------------- |
| Csv File Path | csv までのファイルパス(Assets は除く)  |
| Class Name | Scripable Object のクラス名 |
| Destination | 生成した ScriptableObject を配置するディレクトリへのパス |
| Is Enum | enum かどうか. True の場合は Class Generate と Table Generate は無視される. |
| Class Generate | クラスを生成するかどうか. |
| Table Generate | テーブルクラスとScriptableObjectを生成するかどうか. |
| Key | ScriptableObject の名前をつけるときに利用する一意な値を持つフィールド名 |

## Table Generation
Table は複数の ScriptableObject Data をまとめた ScriptableObject を継承したクラス.
クラス名は Class Name + "Table" になる.
設定で有効な Key をセットしておくと, 自動的に Find メソッドを実装してくれる.

### 例) Human class
```csharp
using UnityEngine;
using System.Collections.Generic;

public class Human : ScriptableObject
{
    public int id;
    public string name;
    public Sprite icon;
    public int hp;
    public float spd;
    public bool isEnemy;
    public int[] scores;
    public HumanType humanType;
    public string[] names;
}
```

Table クラスは以下のようになる.
```csharp
using UnityEngine;
using System.Collections.Generic;

public class HumanTable : ScriptableObject
{
    public List<Human> rows = new List<Human>();

    public Human Find(int key)
    {
        foreach (Human o in rows)
        {
            if (o.id == key)
            {
                return o;
            }
        }
        return null;
    }
}
```
