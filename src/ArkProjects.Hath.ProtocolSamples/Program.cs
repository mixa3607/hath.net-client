// See https://aka.ms/new-console-template for more information

using ArkProjects.Hath.ProtocolSamples;

Console.WriteLine("Hello, World!");
var reqs = new HathRequests("asp");
await reqs.ServerCmd_ThreadedProxyTest_Ok1();
//await reqs.ServerCmd_SpeedTest_Ok1();
//await reqs.ServerCmd_SpeedTest_Ok2();
//await reqs.ServerCmd_SpeedTest_Ok3();
//await reqs.ServerCmd_SpeedTest_Ok4();
//await reqs.ServerCmd_RefreshSettings_Ok();
//await reqs.ServerCmd_RefreshSettings_InvalidTime();
//await reqs.ServerCmd_RefreshCerts_Ok();
//await reqs.ServerCmd_RefreshCerts_InvalidTime();
//await reqs.ServerCmd_StillAlive_Ok();
//await reqs.ServerCmd_StillAlive_InvalidTime();


return;