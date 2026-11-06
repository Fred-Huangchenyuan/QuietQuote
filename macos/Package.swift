// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "QuietQuote",
    platforms: [.macOS(.v13)],
    products: [.executable(name: "QuietQuoteMac", targets: ["QuietQuoteMac"])],
    targets: [
        .target(name: "QuietQuoteCore"),
        .executableTarget(name: "QuietQuoteMac", dependencies: ["QuietQuoteCore"]),
        .testTarget(name: "QuietQuoteCoreTests", dependencies: ["QuietQuoteCore"])
    ],
    swiftLanguageVersions: [.v5]
)
