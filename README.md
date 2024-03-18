# StreamSpace

![VR and desktop interface of StreamSpace](https://github.com/UCL-VR/StreamSpace/assets/1269004/4e2f73f9-3969-4dc9-969d-febbc95dab17)

StreamSpace introduces a framework for exploring screen-based collaborative Mixed Reality (MR) experiences, focusing on the streaming, integration, and layout of screen content within MR environments. By leveraging Unity and Ubiq technologies, this framework facilitates real-time streaming and interaction with dynamically placed screens in a virtual space, enabling users to engage with, reposition, and resize uniquely identified screens.g

## Features

- **Distributed Streaming**: Utilizes Unity and Ubiq for low-latency, peer-to-peer streaming of screen content.
- **Automated Screen Layout**: Incorporates a layout manager for dynamic arrangement of virtual screens within a customizable cylindrical space.
- **Flexible Privacy Settings**: Supports public and private screens, allowing for shared viewing or confidential workspaces.

## Getting Started

To get started with StreamSpace, clone this repository and follow the setup instructions below. *Currently, this repository contains the full code of Ubiq including the necessary modifications to enable video stream tracks.* We will soon upgrade this code to work with the UPM package of Ubiq instead, allowing you to use the code in combination with any supported version of Ubiq.

### Installation

1. Clone the StreamSpace repository.
2. Open the Unity scene located at `Assets/Scenes/Samples/Browser/ScreenStreaming.unity`.
3. Open `Browser/samples/screen_streaming/streaming.html` in a web browser with a local server, such as `http-server`.
4. Run the Unity scene and open the browser-based interface to start streaming content into the virtual space.

## Design and Implementation

StreamSpace is built on Ubiq, utilizing its decentralized, peer-to-peer system for direct client communication via WebRTC. This ensures low-latency interactions essential for real-time VR experiences and reduces bandwidth costs. Unity 3D's robust features for VR development are leveraged for rendering, physics simulations, and user input handling.

### Layout Manager and Dynamic Mesh Generation

The Layout Manager orchestrates the spatial configuration of streamed content, supporting the creation of both curved and flat display meshes. It strategically places multiple views into coherent positions, arranging screens based on their dimensions and maintaining visual harmony within the virtual space.

### Public and Private Screens

StreamSpace differentiates between public and private screens to support various user interactions. Public screens are visible to all users, fostering group activities, while private screens offer a personal area for sensitive tasks, with visibility controls managed through a browser-based interface.

## Usage

After setting up StreamSpace, users can stream 2D window content from any machine into their MR headsets using a browser, facilitating collaborative virtual workspaces. The system allows for intuitive interaction with virtual screens, including repositioning and resizing.

### Toggling Screen Privacy

Users can toggle screens between public and private modes, adapting the environment for solo or collaborative work as needed.

## Contributing

We welcome contributions to StreamSpace! If you're interested in contributing, please fork the repository and create a pull request with your changes.

## Support

For any questions or issues, please create an issue on the GitHub repository or reach out to us through our [Discord server](https://discord.gg/cZYzdcxAAB).