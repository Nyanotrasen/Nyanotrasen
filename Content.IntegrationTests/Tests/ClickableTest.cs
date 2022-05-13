﻿using System;
using System.Threading.Tasks;
using Content.Client.Clickable;
using NUnit.Framework;
using Robust.Client.Graphics;
using Robust.Server.GameObjects;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class ClickableTest : ContentIntegrationTest
    {
        private ClientIntegrationInstance _client;
        private ServerIntegrationInstance _server;

        private const double DirSouth = 0;
        private const double DirNorth = Math.PI;
        private const double DirEast = Math.PI / 2;
        private const double DirSouthEast = Math.PI / 4;
        private const double DirSouthEastJustShy = Math.PI / 4 - 0.1;

        [OneTimeSetUp]
        public async Task Setup()
        {
            (_client, _server) = await StartConnectedServerClientPair(serverOptions: new ServerContentIntegrationOption()
            {
                CVarOverrides =
                {
                    [CVars.NetPVS.Name] = "false"
                }
            });
        }

        [Parallelizable(ParallelScope.None)]
        [Test]
        [TestCase("ClickTestRotatingCornerVisible", 0.25f, 0.25f, DirSouth, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerVisible", 0.35f, 0.5f, DirSouth, 2, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerVisible", -0.25f, -0.25f, DirSouth, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerVisible", 0.25f, 0.25f, DirNorth, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerVisible", -0.25f, -0.25f, DirNorth, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerVisible", -0.25f, 0.25f, DirEast, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerVisible", 0, 0.25f, DirSouthEast, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerVisibleNoRot", 0.25f, 0.25f, DirSouth, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerVisibleNoRot", -0.25f, -0.25f, DirSouth, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerVisibleNoRot", 0.25f, 0.25f, DirNorth, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerVisibleNoRot", -0.25f, -0.25f, DirNorth, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerVisibleNoRot", 0, 0.35f, DirSouthEastJustShy, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerVisibleNoRot", 0.25f, 0.25f, DirSouthEastJustShy, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerInvisible", 0.25f, 0.25f, DirSouth, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerInvisible", 0.35f, 0.5f, DirSouth, 2, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerInvisible", -0.25f, -0.25f, DirSouth, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerInvisible", 0.25f, 0.25f, DirNorth, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerInvisible", -0.25f, -0.25f, DirNorth, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerInvisible", -0.25f, 0.25f, DirEast, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerInvisible", 0, 0.25f, DirSouthEast, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerInvisibleNoRot", 0.25f, 0.25f, DirSouth, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerInvisibleNoRot", -0.25f, -0.25f, DirSouth, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerInvisibleNoRot", 0.25f, 0.25f, DirNorth, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerInvisibleNoRot", -0.25f, -0.25f, DirNorth, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerInvisibleNoRot", 0, 0.35f, DirSouthEastJustShy, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerInvisibleNoRot", 0.25f, 0.25f, DirSouthEastJustShy, 1, ExpectedResult = true)]
        public async Task<bool> Test(string prototype, float clickPosX, float clickPosY, double angle, float scale)
        {
            EntityUid entity = default;
            var clientEntManager = _client.ResolveDependency<IEntityManager>();
            var serverEntManager = _server.ResolveDependency<IEntityManager>();
            var eyeManager = _client.ResolveDependency<IEyeManager>();
            var mapManager = _server.ResolveDependency<IMapManager>();

            await _server.WaitPost(() =>
            {
                var ent = serverEntManager.SpawnEntity(prototype, GetMainEntityCoordinates(mapManager));
                serverEntManager.GetComponent<TransformComponent>(ent).WorldRotation = angle;
                serverEntManager.GetComponent<SpriteComponent>(ent).Scale = (scale, scale);
                entity = ent;
            });

            // Let client sync up.
            await RunTicksSync(_client, _server, 5);

            var hit = false;

            await _client.WaitPost(() =>
            {
                // these tests currently all assume player eye is 0
                eyeManager.CurrentEye.Rotation = 0;

                var pos = clientEntManager.GetComponent<TransformComponent>(entity).WorldPosition;
                var clickable = clientEntManager.GetComponent<ClickableComponent>(entity);

                hit = clickable.CheckClick((clickPosX, clickPosY) + pos, out _, out _);
            });

            await _server.WaitPost(() =>
            {
                serverEntManager.DeleteEntity(entity);
            });

            return hit;
        }
    }
}
