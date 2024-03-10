using System;
using System.IO;
using Content.Shared.Alert;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Tests.Shared.Alert;

[TestFixture, TestOf(typeof(AlertPrototype)), Parallelizable(ParallelScope.Self)]
public sealed class AlertPrototypeTests : ContentUnitTest
{
    private const string Prototypes = @"
- type: alert
  id: HumanHealth
  category: Health
  icons:
  - /Textures/Interface/Alerts/Human/human.rsi/human0.png
  - /Textures/Interface/Alerts/Human/human.rsi/human1.png
  - /Textures/Interface/Alerts/Human/human.rsi/human2.png
  - /Textures/Interface/Alerts/Human/human.rsi/human3.png
  - /Textures/Interface/Alerts/Human/human.rsi/human4.png
  - /Textures/Interface/Alerts/Human/human.rsi/human5.png
  - /Textures/Interface/Alerts/Human/human.rsi/human6.png
  name: Health
  description: ""[color=green]Green[/color] good. [color=red]Red[/color] bad.""
  minSeverity: 0
  maxSeverity: 6";

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IoCManager.Resolve<ISerializationManager>().Initialize();
    }

    [Test]
    public void TestAlertKey()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new AlertKey(AlertType.HumanHealth, null), Is.Not.EqualTo(AlertKey.ForCategory(AlertCategory.Health)));
            Assert.That(new AlertKey(null, AlertCategory.Health), Is.EqualTo(AlertKey.ForCategory(AlertCategory.Health)));
            Assert.That(new AlertKey(AlertType.Buckled, AlertCategory.Health), Is.EqualTo(AlertKey.ForCategory(AlertCategory.Health)));
        });
    }

    [TestCase(0, "/Textures/Interface/Alerts/Human/human.rsi/human0.png")]
    [TestCase(1, "/Textures/Interface/Alerts/Human/human.rsi/human1.png")]
    [TestCase(6, "/Textures/Interface/Alerts/Human/human.rsi/human6.png")]
    public void GetIconPath_SeverityValid_Succeeds(short? severity, string expected)
    {
        var alert = GetTestPrototype();
        Assert.That(alert.GetIcon(severity), Is.EqualTo(new SpriteSpecifier.Texture(new(expected))));
    }

    [TestCase(null)]
    public void GetIconPath_SeverityRequiredButNotProvided_ThrowsArgumentException(short? severity)
    {
        var alert = GetTestPrototype();
        Assert.That(() => alert.GetIcon(severity), Throws.ArgumentException);
    }

    [TestCase(-1)]
    [TestCase(7)]
    public void GetIconPath_SeverityOutOfRange_ThrowsArgumentOutOfRangeException(short? severity)
    {
        var alert = GetTestPrototype();
        Assert.That(() => alert.GetIcon(severity), Throws.InstanceOf(typeof(ArgumentOutOfRangeException)));
    }

    private static AlertPrototype GetTestPrototype()
    {
        using TextReader stream = new StringReader(Prototypes);

        var yamlStream = new YamlStream();
        yamlStream.Load(stream);

        var document = yamlStream.Documents[0];
        var rootNode = (YamlSequenceNode) document.RootNode;
        var proto = (YamlMappingNode) rootNode[0];
        var serMan = IoCManager.Resolve<ISerializationManager>();

        return serMan.Read<AlertPrototype>(new MappingDataNode(proto));
    }
}
