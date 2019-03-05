# DroidServiceTest

To test need to deploy `DroidServcieTest.Droid` and `DroidStarter.Droid`.

`DroidStarter.Droid` only purpose is to start the Android Services.  And to send it Test message.

Issue I am trying to reproduce is in the `DroidServcieTest.Droid` Android Service.  We are noticing that the `Task.Run` instead `Awaiter.Pulse` does not run its action. We see the log statements for start and finish outside the Task.Run but never see the log statements fire inside the TaskRun.  This usually occurs after 6 to 10 hours of usage.  The `Awaiter.Pulse` is called instead the `DroidMessageReceiver.OnReceive`
