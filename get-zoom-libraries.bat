set "repo_name=zoom-c-sharp-wrapper"
set "repo_path=https://github.com/zoom/%repo_name%.git
set "repo_target_name=zoom-custom-ui-wpf"

cd..
git clone %repo_path%

xcopy /s/e/y "%repo_name%\bin" "%repo_target_name%\zoombin\"
