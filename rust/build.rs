fn main() {
    let c = "../c";

    cc::Build::new()
        .flag("-ffunction-sections")
        .flag("-fdata-sections")
        .flag("-w") // suppress warnings from C code
        .define("RPI", None)
        .define("USE_LGPIO_LIB", None)
        .define("DEBUG", None)
        .include(format!("{}/lib/Config", c))
        .include(format!("{}/lib/GUI", c))
        .include(format!("{}/lib/e-Paper", c))
        .include(format!("{}/lib/Fonts", c))
        // HAL / Config
        .file(format!("{}/lib/Config/DEV_Config.c", c))
        .file(format!("{}/lib/Config/dev_hardware_SPI.c", c))
        // GUI
        .file(format!("{}/lib/GUI/GUI_Paint.c", c))
        // Fonts
        .file(format!("{}/lib/Fonts/font8.c", c))
        .file(format!("{}/lib/Fonts/font12.c", c))
        .file(format!("{}/lib/Fonts/font16.c", c))
        .file(format!("{}/lib/Fonts/font20.c", c))
        .file(format!("{}/lib/Fonts/font24.c", c))
        // Display driver
        .file(format!("{}/lib/e-Paper/EPD_2in13_V4.c", c))
        .compile("epd");

    println!("cargo:rustc-link-lib=lgpio");
    println!("cargo:rustc-link-lib=m");
}
