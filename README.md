# simplers
simplers - Simple Reverse Shell

- Windows Only
- Non-interactive

# Usage
- Change `ip`, `port` in `main.cs`
- Compile Project
- On the remote host start `nc -lvnp <port>`
- Start simplers.exe

If you want to change the path for the string and output commands, you can change it in the `inputfile.cs` (compile file and replace it at the root of the project) and `main.cs` files.

# To-Do
- Add Encryption (base64/xor/aes)
