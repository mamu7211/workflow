// Aspire integration tests should not run in parallel
[assembly: Parallelize(Scope = ExecutionScope.MethodLevel, Workers = 1)]
