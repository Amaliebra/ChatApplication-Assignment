﻿using ChatClient.Net.IO;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Text;
using ChatClient.MVVM.Model;
using ChatClient.MVVM.ViewModel;

namespace ChatClient.Net
{
    class Server
    {
        private TcpClient _client;
        public PacketReader PacketReader { get; private set; }
        public event Func<Task> ConnectedEvent;
        public event Action<string> MessageReceivedEvent;
        public event Action DisconnectedEvent;
        public event Action<List<string>> UserListUpdatedEvent;
        private List<string> _connectedUsers = new();
        private MainViewModel _mainViewModel;

        public Server(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }
        public async Task ConnectToServerAsync(string username, int maxRetries = 5, int delayRetries = 2000)
        {
            System.Diagnostics.Debug.WriteLine("Starting connection attempts...");
            int attempt = 0;

            while (attempt < maxRetries)
            {
                try
                {
                    if (_client != null && _client.Connected)
                    {
                        Console.WriteLine("Already connected to the server.");
                        return;
                    }
                    //172.16.17.197 - 192.168.0.20
                    _client = new TcpClient();
                    await _client.ConnectAsync("127.0. 0.1", 5000);
                    PacketReader = new PacketReader(_client.GetStream());

                    _ = Task.Run(() => ReadPacketAsync());

                    await Task.Delay(200);

                    Console.WriteLine($"Transmitting username: {username}");
                    var packetBuilder = new PacketBuilder();
                    packetBuilder.WriteOpCode(1);
                    packetBuilder.WriteString(username);
                    await _client.GetStream().WriteAsync(packetBuilder.GetPacketBytes());

                    if (!_connectedUsers.Contains(username))
                    {
                        _connectedUsers.Add(username);
                        UserListUpdatedEvent?.Invoke(new List<string>(_connectedUsers));
                    }

                    return;
                }
                catch (Exception ex)
                {
                    attempt++;
                    Console.WriteLine($"Connection attempt {attempt} failed: {ex.Message}");
                    await Task.Delay(delayRetries);
                }
            }
        }

        private async Task ReadPacketAsync()
        {
            try
            {
                while (true)
                {
                    var opcode = await PacketReader.ReadOpcodeAsync();
                    switch (opcode)
                    {
                        case 1:
                            if (ConnectedEvent != null)
                                await ConnectedEvent.Invoke();
                            break;
                        case 2:
                            var userListStr = await PacketReader.ReadStringAsync();
                            var users = userListStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                   .ToList();
                            UserListUpdatedEvent?.Invoke(users);
                            break;
                        case 5:
                            var senderUsername = await PacketReader.ReadStringAsync();
                            var messageText = await PacketReader.ReadStringAsync();
                            Console.WriteLine($"[{DateTime.Now}] {messageText}");
                            MessageReceivedEvent?.Invoke($"{senderUsername}: {messageText}");
                            break;
                        case 10:
                            _mainViewModel.UserDisconnected();
                            DisconnectedEvent?.Invoke();
                            return;
                        default:
                            Console.WriteLine($"Unknown opcode: {opcode}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading packet: {ex.Message}");
                _mainViewModel.UserDisconnected();
                DisconnectedEvent?.Invoke();
            }
        }

        public async Task SendMessageAsync(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return;
            }
            if (_client == null || !_client.Connected)
            {
                Console.WriteLine("Not connected to server");
                DisconnectedEvent?.Invoke();
                return;
            }

            try
            {
                await _client.Client.SendAsync(data, SocketFlags.None);
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.SocketErrorCode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
                DisconnectedEvent?.Invoke();
            }
        }
    }
}
