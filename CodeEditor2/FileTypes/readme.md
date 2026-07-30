## FileType

FileType Objectはファイル種別の判別に使用する。
CodeEditor2はファイルを取得する際にこれらのFileType Classを呼んでどの
FileTypeに相当するかを判定し、CreateFileでData.Fileを生成する。

### add new filetypes

// create file type
FileTypes.VerilogHeaderFile fileType = new FileTypes.VerilogHeaderFile();

// register filetype to CodeEditor2.Global.FileTypes
CodeEditor2.Global.FileTypes.Add(fileType.ID, fileType);

## FileClassifyFile

"FileClassify" file. 

