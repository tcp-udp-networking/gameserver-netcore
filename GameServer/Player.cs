using System;
using System.Numerics;

namespace GameServer
{
    class Player
    {
        public int id;
        public string username;
        public Vector3 position;
        public Quaternion rotation;
        public UnityEngine.Texture2D playerTexture2D;

        private float _movementSpeed = 5f / Constants.TICKS_PER_SEC;
        private bool[] _inputs;

        public Player(int _id, string _username, Vector3 _spawnPosition, UnityEngine.Texture2D _playerTexture2D)
        {
            id = _id;
            username = _username;
            position = _spawnPosition;
            rotation = System.Numerics.Quaternion.Identity;
            _inputs = new bool[4];
            playerTexture2D = _playerTexture2D;
        }

        public void Update()
        {
            Vector2 inputDirection = Vector2.Zero;
            if (_inputs[0])
            {
                inputDirection.Y += 1f;
            } 
            if (_inputs[1])
            {
                inputDirection.Y -= 1f;
            } 
            if (_inputs[2])
            {
                inputDirection.X += 1f;
            } 
            if (_inputs[3])
            {
                inputDirection.X -= 1f;
            }

           

            Move(inputDirection);
            ShowWebcamTexture(playerTexture2D);
        }

        private void ShowWebcamTexture(UnityEngine.Texture2D texture)
        {
            playerTexture2D = texture;
            ServerSend.PlayerWebcamTexture(id, this);
        }

        private void Move(Vector2 _inputDirection)
        {
            Vector3 _forward = Vector3.Transform(new Vector3(0, 0, 1), rotation);
            Vector3 _right = Vector3.Normalize(Vector3.Cross(_forward, new Vector3(0, 1, 0)));

            Vector3 _moveDirection = _right * _inputDirection.X + _forward * _inputDirection.Y;
            position += _moveDirection * _movementSpeed;

            ServerSend.PlayerPosition(this);
            ServerSend.PlayerRotation(this);
        }

        public void SetInput(bool[] pInputs, Quaternion pQuaternion)
        {
            _inputs = pInputs;
            rotation = pQuaternion;
        }

        public void SetTexture(UnityEngine.Texture2D texture2D)
        {
            playerTexture2D = texture2D;
        }
    }
}
