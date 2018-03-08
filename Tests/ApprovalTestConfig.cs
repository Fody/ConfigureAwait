#if NET46
using ApprovalTests.Reporters;
[assembly: UseReporter(typeof(AllFailingTestsClipboardReporter),typeof(DiffReporter))]
#endif