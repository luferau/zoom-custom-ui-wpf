# zoom-custom-ui-wpf
Just application example that illustrates how to implement Zoom Custom UI in WPF app

![alt text](/img/zoom-custom-ui-wpf.png)

## steps to app compilation
1. Clone repository

```
git clone https://github.com/luferau/zoom-custom-ui-wpf.git
```

2. Load Zoom SDK libraries 

```
cd zoom-custom-ui-wpf
get-zoom-libraries.bat
```

get-zoom-libraries.bat - this is a simple batch file that clone https://github.com/zoom/zoom-c-sharp-wrapper/ repository and 
copies Zoom SDK libraries to the appropriate directory

```
set "repo_name=zoom-c-sharp-wrapper"
set "repo_path=https://github.com/zoom/%repo_name%.git
set "repo_target_name=zoom-custom-ui-wpf"

cd..
git clone %repo_path%

xcopy /s/e/y "%repo_name%\bin\" "%repo_target_name%\zoombin\"
```

3. Open solution in VS2019

4. Register your app at https://marketplace.zoom.us and generate SDK Key & Secret.
See for help: https://marketplace.zoom.us/docs/guides/build/sdk-app

5. Fill your values in app [CredentialsService](/Services/Credentials/CredentialsService.cs)
 
6. Compile and run
