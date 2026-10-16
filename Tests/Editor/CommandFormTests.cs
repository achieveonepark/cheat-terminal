using Achieve.CheatTerminal.Core;
using NUnit.Framework;

namespace Achieve.CheatTerminal.Tests
{
    public sealed class CommandFormTests
    {
        private static CommandForm Form(string usage)
            => new CommandForm(new DelegateCommand("test", _ => { }, usage: usage));

        [Test]
        public void RequiredParametersPreventExecutionUntilComplete()
        {
            var form = Form("test <x> <y> <z>");
            Assert.That(form.TryBuild(new[] { "1", "", "3" }, out _, out string error), Is.False);
            Assert.That(error, Does.Contain("y"));
            Assert.That(form.TryBuild(new[] { "1", "2", "3" }, out string input, out _), Is.True);
            CollectionAssert.AreEqual(new[] { "1", "2", "3" }, new CommandParser().Parse(input).Args);
        }

        [Test]
        public void OptionalParametersCanBeOmittedButCannotShiftPositions()
        {
            var form = Form("test [name] [count]");
            Assert.That(form.TryBuild(new[] { "", "" }, out string input, out _), Is.True);
            Assert.That(input, Is.EqualTo("test"));
            Assert.That(form.TryBuild(new[] { "", "3" }, out _, out _), Is.False);
        }

        [TestCase("Jane Doe")]
        [TestCase("a \"quoted\" name")]
        [TestCase(@"C:\Games\My Game\")]
        [TestCase("한글 이름")]
        public void ParameterValuesRoundTripWithoutCommandSyntax(string value)
        {
            Assert.That(Form("test <name>").TryBuild(new[] { value }, out string input, out _), Is.True);
            CollectionAssert.AreEqual(new[] { value }, new CommandParser().Parse(input).Args);
        }

        [Test]
        public void GroupedCoordinatesAreSeparateOptionalInputs()
        {
            var form = Form("test <get|pos|reset> <name> [x y z]");
            Assert.That(form.Parameters.Count, Is.EqualTo(5));
            CollectionAssert.AreEqual(new[] { "get", "pos", "reset" }, form.Parameters[0].Suggestions);
            Assert.That(form.Parameters[2].Name, Is.EqualTo("x"));
            Assert.That(form.Parameters[2].Optional, Is.True);
        }

        [Test]
        public void RestArgumentsProduceSeveralTokens()
        {
            var form = Form("test <name> [args]");
            Assert.That(form.TryBuild(new[] { "Player", "0 5 0" }, out string input, out _), Is.True);
            CollectionAssert.AreEqual(new[] { "Player", "0", "5", "0" }, new CommandParser().Parse(input).Args);
        }

        [Test]
        public void VariadicTextSupportsQuotedGroups()
        {
            Assert.That(Form("test <text...>").TryBuild(new[] { "\"hello world\" next" }, out string input, out _), Is.True);
            CollectionAssert.AreEqual(new[] { "hello world", "next" }, new CommandParser().Parse(input).Args);
        }

        [Test]
        public void NoArgumentCommandNeedsOnlyRun()
        {
            var form = Form("test");
            Assert.That(form.Parameters, Is.Empty);
            Assert.That(form.TryBuild(null, out string input, out _), Is.True);
            Assert.That(input, Is.EqualTo("test"));
        }

        [Test]
        public void ParserPreservesEmptyQuotedValuesAndOrdinaryPaths()
        {
            CollectionAssert.AreEqual(new[] { "", @"C:\Games\Player" }, new CommandParser().Parse("test \"\" C:\\Games\\Player").Args);
        }

        [Test]
        public void BuiltFormExecutesRegisteredCommandAndCapturesOutput()
        {
            var terminal = new Terminal();
            terminal.RegisterCommand("gold", ctx => ctx.Output.WriteLine("Gold: " + ctx.GetInt(0), LogLevel.Success), usage: "gold <amount>");
            string result = null;
            ((BufferedOutput)terminal.Output).LineWritten += (text, _) => result = text;
            terminal.Registry.TryGet("gold", out var command);
            Assert.That(new CommandForm(command).TryBuild(new[] { "150" }, out string input, out _), Is.True);
            terminal.Execute(input);
            Assert.That(result, Is.EqualTo("Gold: 150"));
        }
    }
}
