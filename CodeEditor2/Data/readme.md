# CodeEditor2 Data Structure

Project下のフォルダ、ファイルを含む要素をData.Itemとして保持する。
各ItemはUniqueなRelative Pathを持ち、Tree構造でデータを保持する。

Data.Itemはスレッドセーフオブジェクト。どのスレッドからでもデータを取得、更新できる。

上位から下位へは参照を保持し、下位から上位へはWeak Referenceを持つ。
上位から下位への参照が破棄されたときは下位のItemはGabage Colllectorで回収される。


```
                  has reference 
Item    .Items  +-----------------> Item  
                | <- - - - - - - -  
                |   Parent(weak ref)
                |
                |  has reference 
                +-----------------> Item  
                | <- - - - - - - -  
                |   Parent(weak ref)
                |
                |  has reference                       has reference 
                +-----------------> Item    .Items  +-----------------> Item
                | <- - - - - - - -                  | <- - - - - - - -  
                    Parent(weak ref)                    Parent(weak ref)
```


ItemはNavigatePanelNodeを保持している。
NavigatePanelNodeはItem.NavigatePanelNode propertyを呼んだときにCreateNodeメソッドで生成される。

```
           has reference 
Item    +-----------------> NavigatePanelNode  
        <- - - - - - - - - 
          Parent(weak ref)
```

