using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EnvironmentMaker {
    class BillBoard : MonoBehaviour {

        private GameObject player;

        public void SetPlayer(GameObject player) {
            this.player = player;
        }

        void Start() {
        }

        private void Update() {
            if (player != null) {
                var position = this.transform.position;
                var playerPosition = player.transform.position;
                var theta = Math.Atan2(position.z - playerPosition.z, position.x - playerPosition.x);
                this.transform.localRotation = Quaternion.Euler(0, (float)(-theta * 180 / Math.PI + 90), 0);
            }
        }
    }
}
