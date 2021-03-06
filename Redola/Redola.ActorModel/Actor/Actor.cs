﻿using System;
using System.Collections.Generic;
using System.Linq;
using Logrila.Logging;

namespace Redola.ActorModel
{
    public class Actor
    {
        private ILog _log = Logger.Get<Actor>();
        private ActorConfiguration _configuration;
        private IActorDirectory _directory;
        private ActorChannelManager _manager;

        public Actor(ActorConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException("configuration");
            _configuration = configuration;
        }

        public ActorIdentity CenterActor { get { return _configuration.CenterActor; } }
        public ActorIdentity LocalActor { get { return _configuration.LocalActor; } }
        public ActorChannelConfiguration ChannelConfiguration { get { return _configuration.ChannelConfiguration; } }
        public string Type { get { return this.LocalActor.Type; } }
        public string Name { get { return this.LocalActor.Name; } }

        public bool Active
        {
            get
            {
                if (_manager == null)
                    return false;

                var channel = _manager.GetActorChannel(this.LocalActor);
                if (channel == null)
                    return false;
                else
                    return channel.Active;
            }
        }

        public void Bootup()
        {
            var defaultDirectory = new CenterActorDirectory(this.CenterActor, this.ChannelConfiguration);
            Bootup(defaultDirectory);
        }

        public void Bootup(IActorDirectory directory)
        {
            if (directory == null)
                throw new ArgumentNullException("directory");
            if (this.Active)
                throw new InvalidOperationException(
                    string.Format("Local actor [{0}] has already been booted up.", this.LocalActor));

            _log.DebugFormat("Claim local actor [{0}].", this.LocalActor);

            if (_directory != null)
                throw new InvalidOperationException("Actor directory has already been assigned.");
            _directory = directory;

            _manager = new ActorChannelManager(new ActorChannelFactory(_directory, this.ChannelConfiguration));
            _manager.Connected += OnActorConnected;
            _manager.Disconnected += OnActorDisconnected;
            _manager.DataReceived += OnActorDataReceived;

            try
            {
                _manager.ActivateLocalActor(this.LocalActor);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message, ex);
                Shutdown();
                throw new InvalidOperationException(
                    string.Format("Cannot initiate the local actor [{0}] during bootup.", this.LocalActor));
            }

            try
            {
                if (!_directory.Active)
                {
                    _directory.Activate(this.LocalActor);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message, ex);
                Shutdown();
                throw new InvalidOperationException(
                    string.Format("Cannot connect to center actor [{0}] during bootup.", this.CenterActor));
            }
        }

        public void Shutdown()
        {
            if (_manager != null)
            {
                _manager.Connected -= OnActorConnected;
                _manager.Disconnected -= OnActorDisconnected;
                _manager.DataReceived -= OnActorDataReceived;
                _manager.CloseAllChannels();
                _manager = null;
            }
            if (_directory != null)
            {
                _directory.Close();
                _directory = null;
            }
        }

        protected List<ActorIdentity> GetAllActors()
        {
            return _manager.GetAllActors();
        }

        protected virtual void OnActorConnected(object sender, ActorConnectedEventArgs e)
        {
            if (Connected != null)
            {
                Connected(sender, e);
            }
        }

        protected virtual void OnActorDisconnected(object sender, ActorDisconnectedEventArgs e)
        {
            if (Disconnected != null)
            {
                Disconnected(sender, e);
            }
        }

        protected virtual void OnActorDataReceived(object sender, ActorDataReceivedEventArgs e)
        {
            if (DataReceived != null)
            {
                DataReceived(sender, e);
            }
        }

        public event EventHandler<ActorConnectedEventArgs> Connected;
        public event EventHandler<ActorDisconnectedEventArgs> Disconnected;
        public event EventHandler<ActorDataReceivedEventArgs> DataReceived;

        public override string ToString()
        {
            return string.Format("{0}", this.LocalActor);
        }

        #region Send

        public void Send(ActorIdentity remoteActor, byte[] data)
        {
            Send(remoteActor, data, 0, data.Length);
        }

        public void Send(ActorIdentity remoteActor, byte[] data, int offset, int count)
        {
            var channel = _manager.GetActorChannel(remoteActor);
            channel.Send(remoteActor.Type, remoteActor.Name, data, offset, count);
        }

        public void BeginSend(ActorIdentity remoteActor, byte[] data)
        {
            BeginSend(remoteActor, data, 0, data.Length);
        }

        public void BeginSend(ActorIdentity remoteActor, byte[] data, int offset, int count)
        {
            var channel = _manager.GetActorChannel(remoteActor);
            channel.BeginSend(remoteActor.Type, remoteActor.Name, data, offset, count);
        }

        public IAsyncResult BeginSend(ActorIdentity remoteActor, byte[] data, AsyncCallback callback, object state)
        {
            return BeginSend(remoteActor, data, 0, data.Length, callback, state);
        }

        public IAsyncResult BeginSend(ActorIdentity remoteActor, byte[] data, int offset, int count, AsyncCallback callback, object state)
        {
            var channel = _manager.GetActorChannel(remoteActor);
            return channel.BeginSend(remoteActor.Type, remoteActor.Name, data, offset, count, callback, state);
        }

        public void EndSend(ActorIdentity remoteActor, IAsyncResult asyncResult)
        {
            var channel = _manager.GetActorChannel(remoteActor);
            channel.EndSend(remoteActor.Type, remoteActor.Name, asyncResult);
        }

        public void Send(string remoteActorType, string remoteActorName, byte[] data)
        {
            Send(remoteActorType, remoteActorName, data, 0, data.Length);
        }

        public void Send(string remoteActorType, string remoteActorName, byte[] data, int offset, int count)
        {
            var channel = _manager.GetActorChannel(remoteActorType, remoteActorName);
            channel.Send(remoteActorType, remoteActorName, data, offset, count);
        }

        public void BeginSend(string remoteActorType, string remoteActorName, byte[] data)
        {
            BeginSend(remoteActorType, remoteActorName, data, 0, data.Length);
        }

        public void BeginSend(string remoteActorType, string remoteActorName, byte[] data, int offset, int count)
        {
            var channel = _manager.GetActorChannel(remoteActorType, remoteActorName);
            channel.BeginSend(remoteActorType, remoteActorName, data, offset, count);
        }

        public IAsyncResult BeginSend(string remoteActorType, string remoteActorName, byte[] data, AsyncCallback callback, object state)
        {
            return BeginSend(remoteActorType, remoteActorName, data, 0, data.Length, callback, state);
        }

        public IAsyncResult BeginSend(string remoteActorType, string remoteActorName, byte[] data, int offset, int count, AsyncCallback callback, object state)
        {
            var channel = _manager.GetActorChannel(remoteActorType, remoteActorName);
            return channel.BeginSend(remoteActorType, remoteActorName, data, offset, count, callback, state);
        }

        public void EndSend(string remoteActorType, string remoteActorName, IAsyncResult asyncResult)
        {
            var channel = _manager.GetActorChannel(remoteActorType, remoteActorName);
            channel.EndSend(remoteActorType, remoteActorName, asyncResult);
        }

        public void Send(string remoteActorType, byte[] data)
        {
            BeginSend(remoteActorType, data, 0, data.Length);
        }

        public void Send(string remoteActorType, byte[] data, int offset, int count)
        {
            var channel = _manager.GetActorChannel(remoteActorType);
            channel.Send(remoteActorType, data, offset, count);
        }

        public void BeginSend(string remoteActorType, byte[] data)
        {
            BeginSend(remoteActorType, data, 0, data.Length);
        }

        public void BeginSend(string remoteActorType, byte[] data, int offset, int count)
        {
            var channel = _manager.GetActorChannel(remoteActorType);
            channel.BeginSend(remoteActorType, data, offset, count);
        }

        #endregion

        #region Broadcast

        public void Broadcast(string remoteActorType, byte[] data)
        {
            Broadcast(remoteActorType, data, 0, data.Length);
        }

        public void Broadcast(string remoteActorType, byte[] data, int offset, int count)
        {
            var channels = _manager.GetActorChannels(remoteActorType);
            foreach (var channel in channels.Where(c => c != null))
            {
                channel.Send(remoteActorType, data, offset, count);
            }
        }

        public void BeginBroadcast(string remoteActorType, byte[] data)
        {
            BeginBroadcast(remoteActorType, data, 0, data.Length);
        }

        public void BeginBroadcast(string remoteActorType, byte[] data, int offset, int count)
        {
            var channels = _manager.GetActorChannels(remoteActorType);
            foreach (var channel in channels.Where(c => c != null))
            {
                channel.BeginSend(remoteActorType, data, offset, count);
            }
        }

        #endregion
    }
}
