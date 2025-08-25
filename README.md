very simple attempt at making a eventpublisher, that auto maps events with listeners on startup, and will trigger subscribers when event is published.

currently only supports events containing simple types like int,string,long,double,bool. no objects for now

<h5>TODO:</h5>
     design a test suite and setup proper test cases.
     test for dependency injection,
     missing properties should/shouldnot throw?,
     what should happen if something throws?,
     monitoring setup with opentelemetry
     better test facade for package consumers
