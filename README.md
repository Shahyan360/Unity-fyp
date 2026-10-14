# Embodied AI Unity Experience

Unity `6000.6.2f1` project with three locally guided worlds: Ancient Rome, a science classroom, and a café conversation/order-practice scene.

## Run

1. Open this folder in Unity `6000.6.2f1` and wait for the initial import and script compile to finish.
2. Start the local services using [`embodied-ai-backend/README.md`](embodied-ai-backend/README.md). No cloud API key is used.
3. Open `Assets/Scenes/HomeScene.unity` if the Lumen hub is not already open, then press Play.
4. Choose a world card. Use WASD to move. The cursor starts free so you can use the guide; right-click on the 3D world to enter mouse-look, then press `Esc` to release it. Left-click remains available for buttons. Press `E` while looking at an object/person to ask about it. The Worlds button returns to the hub.

The café menu is a simulation and does not charge money. See [`PROJECT_COMPLETION.md`](PROJECT_COMPLETION.md) for feature coverage, controls, test results, and remaining production work.
