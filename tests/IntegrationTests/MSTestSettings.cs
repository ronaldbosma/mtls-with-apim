// Set parallel execution for MSTest to class level to avoid issues with SSL connections in parallel tests.
//
// Setting it to method level can cause the following exception to occur on random tests due to multiple tests trying to establish SSL connections simultaneously:
//   System.Net.Http.HttpRequestException: The SSL connection could not be established, see inner exception.
//   ---> System.Security.Authentication.AuthenticationException: Authentication failed because the remote party sent a TLS alert: 'ProtocolVersion'.
//   ---> System.ComponentModel.Win32Exception: The message received was unexpected or badly formatted.
[assembly: Parallelize(Scope = ExecutionScope.ClassLevel)]
