using DenOfIz;

namespace NiziKit.UI.Widgets;

public class Immediate : Widget
{
    public Immediate(Action declare)
    {
        Declare = declare;
    }

    public Action Declare { get; set; }

    protected override void OnBuild()
    {
        ref var desc = ref Desc;
        if (Visible && !HasCustomLayout)
        {
            desc.Layout.Display = UIDisplay.Contents;
        }

        using (OpenSelf())
        {
            Declare();
        }
    }
}
