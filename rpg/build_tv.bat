@echo off
rem Configure environment for MSVC x64
call "C:\BuildTools\Common7\Tools\VsDevCmd.bat" -arch=x64 -host_arch=x64

rem Run CMake configure and build
"C:\Program Files\CMake\bin\cmake.exe" -S "%~dp0" -B "%~dp0build" -G "NMake Makefiles" ^
  -DUSE_WGPU_NATIVE=1 ^
  -DWGPU_NATIVE_PATH=C:/Deps/wgpu-native ^
  -DSDL3_LIB_DIR=C:/Deps/SDL3/lib/x64 ^
  -DSDL3_INCLUDE_DIR=C:/Deps/SDL3/include

"C:\Program Files\CMake\bin\cmake.exe" --build "%~dp0build" --config Release


