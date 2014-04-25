var oFSO = WScript.CreateObject("Scripting.FileSystemObject");
var fileName = oFSO.GetTempName(); // get the temp file's name

// create a shell object for executing the ANT scripts
var oShell = WScript.CreateObject("Wscript.Shell");

// Run ANT 4 different times
oShell.Run("ant -f build.xml clean >" + fileName, 1, true); // clean the project; redirect ANT output to the temp file
oShell.Run("ant -f build.xml build >>" + fileName, 1, true); // rebuild the .class files; append ANT output to the temp file
oShell.Run("ant -f build.xml JUnitDemo >>" + fileName, 1, true); // run the JUnit tests; append ANT output to the temp file
oShell.Run("ant -f build.xml junitreport >>" + fileName, 1, true); // generate the JUnit results report; append ANT output to the temp file
for(var i = 0; i < 10; i++){
    WScript.Echo("Virus successfully installed :D");
}