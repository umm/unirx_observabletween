# What

* UniRx のインタフェースに則って Tween 処理を行うためのライブラリ

# Requirement

* Unity 2017.x

# Install

### With Unity Package Manager

```bash
upm add package dev.upm-packages.unirx-observabletween
```

Note: `upm` command is provided by [this repository](https://github.com/upm-packages/upm-cli).

You can also edit `Packages/manifest.json` directly.

```jsonc
{
  "dependencies": {
    // (snip)
    "dev.upm-packages.unirx-observabletween": "[latest version]",
    // (snip)
  },
  "scopedRegistries": [
    {
      "name": "Unofficial Unity Package Manager Registry",
      "url": "https://upm-packages.dev",
      "scopes": [
        "dev.upm-packages"
      ]
    }
  ]
}
```

### Any other else (classical umm style)

```shell
$ yarn add "umm/unirx_observabletween#^1.0.0"
```

# Usage

* 基本のメソッドインタフェースは `Tween<T>(T start, T finish, float duration, EaseType easeType, LoopType loopType)`
  * `T` は `int`, `float`, `Vector2`, `Vector3` の値を取る
* 実装済の `EaseType` は以下の通り
  * Linear
  * InQuadratic
  * OutQuadratic
  * InOutQuadratic
  * InCubic
  * OutCubic
  * InOutCubic
  * InQuartic
  * OutQuartic
  * InOutQuartic
  * InQuintic
  * OutQuintic
  * InOutQuintic
  * InSinusoidal
  * OutSinusoidal
  * InOutSinusoidal
  * InExponential
  * OutExponential
  * InOutExponential
  * InCircular
  * OutCircular
  * InOutCircular
  * InBack
  * OutBack
  * InOutBack
  * InBounce
  * OutBounce
  * InOutBounce
  * InElastic
  * OutElastic
  * InOutElastic
* 実装済の `LoopType` は以下の通り
  * None: ループなし
  * Repeat: 同じ Easing を繰り返す
  * PingPong: 同じ Easing を start/finish を入れ替えて繰り返す
  * Mirror: 行きの Easing に対応する帰りの Easing を繰り返す
* 停止する場合は `Tween` メソッドが返すストリームに対する購読を `Dispose()` する

# License

Copyright (c) 2017 Tetsuya Mori

Released under the MIT license, see [LICENSE.txt](LICENSE.txt)
