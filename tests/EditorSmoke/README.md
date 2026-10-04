# Native code editor smoke test

Run on Windows with the WebView2 runtime installed:

```powershell
dotnet build tests/EditorSmoke -o build/editor-smoke
& ./build/editor-smoke/EditorSmoke.exe
```

Run the executable rather than `dotnet EditorSmoke.dll` so WebView2's default
profile directory is beside the test executable, in a writable build directory.
The test creates its own offscreen Builder, sends a real WebView2 command to
open Custom Properties, and checks CodeMirror readiness and initial source.
The browser tests verify line numbers, syntax colors and formatting. This test
does not interact with an existing Builder session.

WebView2 callbacks must return before opening modal UI. Designer commands and
editor commands are posted to WinForms with BeginInvoke to avoid the nested
message loop that previously left the editor blank.
