using System;
using BepInEx;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.SceneManagement;
using VirtualStumpRestored.Core;

namespace VirtualStumpRestored;

[BepInPlugin("smoothe.virtualstump.restored", "Virtual Stump Restored", "1.0.1")]
[DefaultExecutionOrder(-10000)]
public sealed class Plugin : BaseUnityPlugin
{
    private static Plugin? instance;
    private readonly HeadCountdown countdown = new HeadCountdown();
    private readonly LobbyMonitor lobby = new LobbyMonitor();
    private ModelAssets? assets;
    private NativeTeleport? teleport;
    private TeleporterBinding? binding;
    private TeleporterView? view;
    private GTPlayer? player;
    private float nextLookup;
    private string? lastFailure;
    private bool suspended;
    private bool started;

    private void Awake()
    {
        if (instance != null && instance != this) { enabled = false; return; }
        instance = this;
    }

    private void OnEnable()
    {
        if (instance != this) return;
        try
        {
            assets = new ModelAssets();
            teleport = new NativeTeleport();
            lobby.Changed += OnLobbyChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            countdown.Reset();
            nextLookup = 0;
            started = true;
            Logger.LogInfo("Loaded supplied teleporter mesh. Waiting for the Stump scene.");
        }
        catch (Exception ex)
        {
            Logger.LogError("Cannot initialize Virtual Stump Restored: " + ex.Message);
            enabled = false;
        }
    }

    private void Update()
    {
        if (!started) return;
        try
        {
            lobby.Refresh();
            teleport?.ObserveCompletion();
            if (player != GTPlayer.Instance)
            {
                view?.ReleaseHands();
                player = GTPlayer.Instance;
                CancelCountdown();
            }
            if (view == null || !view.Valid)
            {
                DisposeView();
                if (Time.unscaledTime >= nextLookup) TryCreateView();
                return;
            }
            bool active = view.Active && player != null && player.headCollider != null &&
                          player.mainCamera != null && player.mainCamera.isActiveAndEnabled && !suspended;
            view.SetPhysics(active && lobby.PhysicsAllowed && teleport != null && !teleport.InFlight);
            if (!active) { CancelCountdown(); return; }
            bool inside = view.HeadOverlaps(player!.headCollider!);
            bool allowed = lobby.Kind != LobbyKind.Unavailable && teleport != null && teleport.CanEnter;
            bool finished = countdown.Tick(Time.realtimeSinceStartupAsDouble, inside, allowed);
            view.ShowCountdown(countdown.Number, player.mainCamera);
            if (finished) StartTeleport();
        }
        catch (Exception ex)
        {
            ReportFailure("Teleporter suspended: " + ex.Message);
            DisposeView();
            nextLookup = Time.unscaledTime + 5;
        }
    }

    private void FixedUpdate()
    {
        if (!started) return;
        try
        {
            // Recheck before physics as well as on network events. No scene searches here.
            lobby.Refresh();
            if (!lobby.PhysicsAllowed || suspended) view?.SetPhysics(false);
        }
        catch (Exception ex)
        {
            view?.SetPhysics(false);
            ReportFailure("Collision disabled: " + ex.Message);
        }
    }

    private void TryCreateView()
    {
        nextLookup = Time.unscaledTime + 5;
        if (assets == null) return;
        binding = TeleporterBinding.Find();
        if (binding == null)
        {
            ReportFailure("The original Stump teleporter references are not loaded. Will retry after scene changes.");
            return;
        }
        view = new TeleporterView(binding, assets.Mesh, assets.CountdownShader, lobby);
        lastFailure = null;
        countdown.CancelUntilExit();
        Logger.LogInfo("Restored the supplied teleporter beside the Stump computer desk.");
    }

    private void StartTeleport()
    {
        if (view == null || binding == null || teleport == null) return;
        view.SetPhysics(false);
        view.ReleaseHands();
        view.ShowCountdown(0, null);
        lobby.Refresh();
        if (lobby.Kind == LobbyKind.Unavailable || !teleport.CanEnter) return;
        if (!teleport.TryEnter(binding.NativeTeleporter, success =>
            {
                if (this == null) return;
                if (!success) ReportFailure("The game declined the Virtual Stump transition. Exit the headset to retry.");
            }))
            ReportFailure("Virtual Stump is not ready. Exit the headset and retry when the game finishes loading.");
    }

    private void OnLobbyChanged()
    {
        if (!lobby.PhysicsAllowed) view?.SetPhysics(false);
        CancelCountdown();
    }
    private void CancelCountdown() { countdown.CancelUntilExit(); view?.ShowCountdown(0, null); }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { nextLookup = 0; CancelCountdown(); }
    private void OnSceneUnloaded(Scene scene) { nextLookup = 0; CancelCountdown(); if (view != null && !view.Valid) DisposeView(); }
    private void OnApplicationPause(bool paused) { suspended = paused; if (paused) { view?.SetPhysics(false); CancelCountdown(); } }

    private void ReportFailure(string message)
    {
        if (message == lastFailure) return;
        lastFailure = message;
        Logger.LogWarning(message);
    }

    private void DisposeView()
    {
        view?.Dispose();
        view = null;
        binding = null;
        countdown.CancelUntilExit();
    }

    private void OnDisable()
    {
        if (instance != this) return;
        started = false;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        lobby.Changed -= OnLobbyChanged;
        lobby.Dispose();
        DisposeView();
        assets?.Dispose();
        assets = null;
        player = null;
    }
    private void OnDestroy() { if (instance == this) instance = null; }
}
