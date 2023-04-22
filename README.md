# MoraAudioModify

直接将文件或文件夹拖到exe上即可完成删Tag+改名 改名格式按照歌曲名-歌手名<br>
```
Usage:<br>
  MoraAudioModify <url> [options]

Arguments:
  <url>  Mora音频文件或文件夹路径

Options:<br>
  -scf, --skip-change-filename     跳过修改文件名
  -sff, --skip-Filename-Filtering  跳过纯数字文件名过滤
  -debug, --debug-log              启用Debug日志输出
```
示例<br>
```
MoraAudioModify G:\Music -scf -sff -debug
```
已知Bug：处理文件夹，多线程下输出会乱掉不影响结果，也懒得修了
