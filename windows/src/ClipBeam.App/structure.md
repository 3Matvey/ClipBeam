src/
  ClipBeam.App/
    ClipBeam.App.csproj
    Program.cs
    appsettings.json
    appsettings.Development.json

    Composition/
      ServiceCollectionExtensions.cs
      LoggingExtensions.cs

    Hosting/
      AppHost.cs
      BackgroundServiceExtensions.cs

    Tray/
      TrayAppContext.cs
      TrayMenuBuilder.cs
      TrayCommands.cs

    Features/
      Pairing/
        Contracts/
          PairingPayload.cs
          PairingTicket.cs
          PairingResult.cs

        GenerateQr/
          GenerateQrHandler.cs
          GenerateQrRequest.cs
          GenerateQrResult.cs

        PairFromQr/
          PairFromQrHandler.cs
          PairFromQrRequest.cs

        CreatePairingTicket/
          CreatePairingTicketHandler.cs

        ValidatePairingTicket/
          ValidatePairingTicketHandler.cs

        Infrastructure/
          QrCodeGenerator.cs
          InMemoryPairingTicketStore.cs
          PairingEndpointProvider.cs
          PairingPayloadCodec.cs

      Discovery/
        Contracts/
          DiscoveredDevice.cs
          ServiceAnnouncement.cs

        AdvertiseLocalService/
          AdvertiseLocalServiceWorker.cs

        DiscoverPeers/
          DiscoverPeersWorker.cs
          DiscoveryCache.cs

        ResolvePeer/
          ResolvePeerHandler.cs

        Infrastructure/
          MdnsServiceAdvertiser.cs
          MdnsPeerDiscovery.cs
          LocalNetworkInfo.cs

      Clipboard/
        Contracts/
          ClipboardSnapshot.cs
          ClipboardChanged.cs

        WatchClipboard/
          ClipboardWatcher.cs
          ClipboardChangedHandler.cs
          ClipboardWindow.cs
          StaThreadRunner.cs

        ReadClipboard/
          ReadClipboardHandler.cs

        ApplyRemoteClipboard/
          ApplyRemoteClipboardHandler.cs

        History/
          ClipboardHistoryStore.cs
          GetClipboardHistoryHandler.cs

        Infrastructure/
          WindowsClipboardAdapter.cs

      Sync/
        Contracts/
          SyncEnvelope.cs
          SyncChunk.cs
          SyncTransfer.cs

        PushClip/
          PushClipHandler.cs

        ReceiveClip/
          ReceiveClipHandler.cs

        AssembleChunks/
          ChunkAssembler.cs

        PreventEcho/
          EchoGuard.cs

        CoordinateSync/
          SyncCoordinator.cs
          TransferManager.cs

      Devices/
        Contracts/
          DeviceRecord.cs
          DeviceConnectionInfo.cs

        RegisterDevice/
          RegisterDeviceHandler.cs

        ListDevices/
          ListDevicesHandler.cs

        RemoveDevice/
          RemoveDeviceHandler.cs

        UpdateDeviceState/
          UpdateDeviceStateHandler.cs

        Infrastructure/
          JsonFileDeviceStore.cs
          DeviceRegistry.cs

      Transport/
        Grpc/
          Contracts/
            clipbeam.proto

          Server/
            ClipSyncGrpcService.cs
            PairingGrpcService.cs

          Client/
            ClipSyncGrpcClient.cs
            PairingGrpcClient.cs
            GrpcChannelFactory.cs

          Mapping/
            GrpcMappings.cs

          Hosting/
            GrpcServerHost.cs

      Settings/
        GetSettings/
          GetSettingsHandler.cs

        UpdateSettings/
          UpdateSettingsHandler.cs

        Contracts/
          AppSettings.cs

      Notifications/
        NotifyInfo/
          NotifyInfoHandler.cs
        NotifyError/
          NotifyErrorHandler.cs
        Infrastructure/
          ToastNotifier.cs

    Core/
      Models/
        Clip.cs
        ClipContent.cs
        ClipMeta.cs
        Device.cs
        Capabilities.cs
        Hash.cs

      Enums/
        ContentType.cs
        Platform.cs
        AuthScheme.cs
        ChunkCompression.cs
        HashAlgo.cs
        ImageFormat.cs
        ExifOrientation.cs

      Text/
        TextNormalization.cs

      Images/
        ImageMeta.cs
        ImageClipContent.cs

      Hashing/
        Sha256Hasher.cs

      Results/
        Result.cs

      Errors/
        DomainException.cs

    Storage/
      clips/
      devices/

    Assets/
      app.ico