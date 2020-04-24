# procon2020-AI-A
第31回全国高等専門学校プログラミングコンテスト 競技部門 AI Program. Developerd by 松江高専 Renju Aoki, Go Suzuki, Naoto Kawakami, and some members.

## Requirements & Develop Environment

> OS : Windows 10 2019 Update  
> CPU : Haswell or later Intel CPUs, Ryzen  
> Memory : more than 2GBytes  
  
> Softwares  
> VisualStudio 2019 or later  
> .Net Core 3.0 or later  
> C# 7.3 or later  

## How to use Git?
### はじめ
#### git clone https://github.com/mct-procon/procon2016
 これでリポジトリをクローン．
#### git branch ブランチの名前
 これで，ローカルにブランチつくり，
#### git checkout ブランチの名前
 ブランチ変更
#### git push origin ブランチの名前
 リモートリポジトリにブランチ作成
### アップロード
#### git add *
 変更内容をまとめて
#### git commit -a -m "コメント"
 変更内容をコミットし，
#### git push origin ブランチ名
 リモートリポジトリに反映する．
# 基本的なコマンド
### [最初のみ] git clone https://github.com/mct-procon/procon2016
 リポジトリをローカルにダウンロードする． 
## BRANCH
### git branch ブランチの名前
 ブランチを作成します． 
### git checkout ブランチの名前
 ブランチを移動します． 
### git branch
 ローカルのブランチの一覧を表示します． 
### git branch -d ブランチの名前
 ブランチを削除します． 
### git branch -r
 リモートのブランチの一覧を表示します．
## COMMIT
### git add *
 編集・追加内容をステージングにあげます．
### git commit -a -m "コメント"
 コミットします．
### git push　(たいていはあとのように) origin ブランチ名
 リモートリポジトリに変更内容(コミットたち)を送ります． 
## UPDATE
### git pull
 リモートリポジトリから変更内容(コミットたち)を受け取り，ローカルリポジトリに反映します．