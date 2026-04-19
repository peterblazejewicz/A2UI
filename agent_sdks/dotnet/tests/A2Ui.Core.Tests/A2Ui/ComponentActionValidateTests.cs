using A2Ui.Core.Components;
using A2Ui.Core.Messages;

namespace A2Ui.Core.Tests.A2Ui;

public sealed class ComponentActionValidateTests
{
    [Fact]
    public void Validate_EventOnly_Succeeds()
    {
        var action = new ComponentAction { Event = new ActionEvent { Name = "click" } };

        action.Validate();
    }

    [Fact]
    public void Validate_FunctionCallOnly_Succeeds()
    {
        var action = new ComponentAction { FunctionCall = new FunctionCallValue { Call = "echo" } };

        action.Validate();
    }

    [Fact]
    public void Validate_Empty_Throws()
    {
        var action = new ComponentAction();

        var ex = Assert.Throws<A2UiMessageValidationException>(() => action.Validate());

        Assert.Contains("neither", ex.Message);
    }

    [Fact]
    public void Validate_Both_Throws()
    {
        var action = new ComponentAction
        {
            Event = new ActionEvent { Name = "click" },
            FunctionCall = new FunctionCallValue { Call = "echo" },
        };

        var ex = Assert.Throws<A2UiMessageValidationException>(() => action.Validate());

        Assert.Contains("both", ex.Message);
    }
}
