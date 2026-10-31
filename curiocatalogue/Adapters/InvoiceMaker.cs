using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using CurioCatalogue.Core;
using ExhaustiveMatching;

namespace CurioCatalogue.Adapters;

/// <summary>Why no invoice was made; the code also names the player-facing message ("ingameerror-" + code).</summary>
public enum MakeRefusal { Unsupported, NoAccess, LedgerFull, NoDifference, NoSheets }

[Closed(typeof(MakeInvoiceResult.Made), typeof(MakeInvoiceResult.Refused))]
public abstract record MakeInvoiceResult
{
    /// <param name="InvoiceStack">The new invoice item, still to be handed to the player.</param>
    public sealed record Made(ItemStack InvoiceStack, Invoice Invoice, int SheetsLeft) : MakeInvoiceResult;

    public sealed record Refused(MakeRefusal Reason) : MakeInvoiceResult;
}

/// <summary>
/// Makes an invoice from a ledger and a container (domain.md, Making an invoice). Runs on the server. Changes the ledger
/// stack only when an invoice is made; the caller marks the slot dirty and gives the invoice to the player.
/// </summary>
public sealed class InvoiceMaker
{
    private readonly IWorldAccessor world;
    private readonly LedgerStore store;
    private readonly ContainerReader reader;
    private readonly ContainerAccess access;
    private readonly Func<LedgerLimits> limits;

    public InvoiceMaker(IWorldAccessor world, LedgerStore store, ContainerReader reader, ContainerAccess access, Func<LedgerLimits> limits)
    {
        this.world = world;
        this.store = store;
        this.reader = reader;
        this.access = access;
        this.limits = limits;
    }

    public MakeInvoiceResult Make(IPlayer player, ItemStack ledgerStack, LedgerMode mode, InvoiceKind kind, BlockPos pos)
    {
        pos = reader.MainPos(pos);
        // "Unsupported" first: it says nothing about the contents, so it may come before the access check.
        var be = world.BlockAccessor.GetBlockEntity(pos);
        if (reader.Classify(be) != ContainerKind.Supported) return new MakeInvoiceResult.Refused(MakeRefusal.Unsupported);
        if (!access.CanUse(player, pos)) return new MakeInvoiceResult.Refused(MakeRefusal.NoAccess);

        var stacks = reader.Read(be)!;

        var slots = stacks.Where(s => !IsOurs(s)).Select(store.ToEntry);
        var ledger = store.ReadLedger(ledgerStack, mode);
        var result = ledger.CreateInvoice(kind, slots, limits(), world.Calendar.TotalDays, new BlockPosition(pos.X, pos.Y, pos.Z));
        return Finish(result, kind, ledgerStack);
    }

    /// <summary>
    /// A want list from an album (domain.md, Albums and collections): its "missing" list, for one of its sheets. No container
    /// is involved; <paramref name="where"/> is only noted on the invoice.
    /// </summary>
    public MakeInvoiceResult MakeWantList(ItemStack albumStack, BlockPos where)
    {
        var album = store.ReadLedger(albumStack, LedgerMode.Album);
        var result = album.MakeWantList(world.Calendar.TotalDays, new BlockPosition(where.X, where.Y, where.Z), store.CollectionOf(album));
        return Finish(result, InvoiceKind.WantList, albumStack);
    }

    private MakeInvoiceResult Finish(CreateInvoiceResult result, InvoiceKind kind, ItemStack ledgerStack)
    {
        switch (result)
        {
            case CreateInvoiceResult.Refused refused:
                return new MakeInvoiceResult.Refused(refused.Reason switch
                {
                    InvoiceRefusal.LedgerFull => MakeRefusal.LedgerFull,
                    InvoiceRefusal.NoDifference => MakeRefusal.NoDifference,
                    InvoiceRefusal.NoSheets => MakeRefusal.NoSheets,
                    _ => throw ExhaustiveMatch.Failed(refused.Reason),
                });

            case CreateInvoiceResult.Created created:
                var invoiceItem = world.GetItem(InvoiceCode(kind))
                    ?? throw new InvalidOperationException($"Item {InvoiceCode(kind)} is not registered");
                var invoiceStack = new ItemStack(invoiceItem);
                store.WriteInvoice(invoiceStack, created.Invoice);
                store.WriteLedger(ledgerStack, created.Ledger);
                return new MakeInvoiceResult.Made(invoiceStack, created.Invoice, created.Ledger.Sheets);

            default:
                throw ExhaustiveMatch.Failed(result);
        }
    }

    public static AssetLocation InvoiceCode(InvoiceKind kind) =>
        new(CurioCatalogueModSystem.ModId, "invoice-" + kind.ToString().ToLowerInvariant());

    /// <summary>The mod's own items (ledgers, albums, invoices) never go on an invoice (domain.md, Books).</summary>
    private static bool IsOurs(ItemStack stack) => stack.Collectible?.Code?.Domain == CurioCatalogueModSystem.ModId;
}
