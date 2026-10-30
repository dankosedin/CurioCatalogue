using System;
using Vintagestory.API.Client;

namespace CurioCatalogue.Gui;

/// <summary>
/// Vanilla's confirmation, made once per question: like our windows it leaves LoadedGuis and frees its composer when
/// closed. Vanilla's own stays loaded until the game quits (it keeps UnregisterOnClose false, vsapi GuiDialog.cs:260).
/// </summary>
public sealed class ConfirmDialog(ICoreClientAPI capi, string text, Action<bool> onAnswer) : GuiDialogConfirm(capi, text, onAnswer)
{
    public override bool UnregisterOnClose => true;

    public override void OnGuiClosed()
    {
        base.OnGuiClosed();
        Dispose();
    }
}
