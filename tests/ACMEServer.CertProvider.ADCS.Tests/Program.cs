using Windows.Win32;
using Windows.Win32.Security.Cryptography.Certificates;

//PInvoke.CoCreateInstance<ICertAdmin>()

var certRequest = CCertRequest.CreateInstance<ICertRequest3>();
var certAdmin = CCertAdmin.CreateInstance<ICertAdmin>();


