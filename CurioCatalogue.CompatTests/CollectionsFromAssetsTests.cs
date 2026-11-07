using CurioCatalogue.Adapters;
using CurioCatalogue.Core;
using Vintagestory.API.Common;

namespace CurioCatalogue.CompatTests;

/// <summary>
/// K3 of the collections core plan: the registry reads the definitions of every domain, with the patches applied. Our test mod
/// <c>cccollections</c> (<c>testmods/</c>) brings a collection of the game's ingots by a definition of its own domain, and a patch
/// of that definition (the way an admin or another mod changes one) leaves out iron. The vanilla tapestries stay the collection
/// of 0.1: the same pieces and pictures.
/// </summary>
[Collection(CompatCollection.Name)]
public class CollectionsFromAssetsTests(CompatServerFixture server) : BookTestBase(server)
{
    [Fact]
    public void A_definition_of_another_domain_is_a_collection_of_the_registered_variants()
    {
        var seen = game.OnMain(() =>
        {
            var members = Mod.Collections.Defined("ingots")!.Level("ingots")!.Members.Select(m => m.Id).Order().ToList();
            var registered = World.Collectibles.Where(c => c.Code.Domain == "game" && c.Code.Path.StartsWith("ingot-"))
                .Select(c => c.Code.Path["ingot-".Length..]).Where(m => !m.Contains('-')).Order().ToList();
            return (members, registered);
        });

        Assert.Contains("copper", seen.registered); // the game's ingots are there to compare with
        // Iron is the patch's doing: the definition itself leaves nothing out.
        Assert.Equal(seen.registered.Where(m => m != "iron"), seen.members);
    }

    [Fact]
    public void A_patch_of_a_definition_is_in_what_the_registry_reads()
    {
        var exclude = game.OnMain(() => Mod.Collections.Defined("ingots")!.Definition.Exclude);

        Assert.Equal(["ingot-iron"], exclude);
    }

    [Fact]
    public void The_vanilla_tapestries_are_the_collection_of_0_1_with_the_picture_of_the_test_mod()
    {
        // 88 pieces in 31 pictures in the game (modding.md, Vanilla content we rely on) and cctapestry's 2×1 picture.
        var (pieces, pictures) = game.OnMain(() =>
        {
            var members = Mod.Collections.Get(CollectionRegistry.TapestriesId)!.Members;
            return (members.Count, members.Select(m => m.Group).Distinct().Count());
        });

        Assert.Equal((88 + 2, 31 + 1), (pieces, pictures));
    }

    [Fact]
    public void Every_collection_of_the_registry_is_found_by_its_samples_and_by_nothing_else()
    {
        var seen = game.OnMain(() => (
            Mod.Collections.ForSample(Tapestry("ambush1"))?.Id,
            Mod.Collections.ForSample(Vanilla("ingot-copper"))?.Id,
            Mod.Collections.ForSample(Vanilla("ingot-iron"))?.Id));

        // The ingot collection has no MVP view that finds it by sample before K4? It has: any registered member.
        Assert.Equal(("tapestries", "ingots", null), seen);
    }
}
