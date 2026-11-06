import XCTest
@testable import QuietQuoteCore

final class MarketTests: XCTestCase {
    func ticker(_ price: String = "7.0300", id: String = "KORU-USDT-SWAP", ts: String = "1793944800000") -> Data {
        Data("{\"code\":\"0\",\"data\":[{\"instId\":\"\(id)\",\"instType\":\"SWAP\",\"last\":\"\(price)\",\"ts\":\"\(ts)\"}]}".utf8)
    }
    func testInstrumentIdentityAndUnits() {
        XCTAssertEqual(Market("BTC-USD-SWAP")?.unit, "USD")
        XCTAssertEqual(Market("KORU-USDT-SWAP")?.coin, "KORU")
        XCTAssertNil(Market("BTC-USDT")); XCTAssertNil(Market("btc-USDT-SWAP"))
    }
    func testQuotePrecisionAndFreshness() throws {
        let q = try Quote.parse(ticker(), expected: "KORU-USDT-SWAP")
        XCTAssertEqual(q.text, "7.03")
        XCTAssertFalse(q.isStale(now: q.time.addingTimeInterval(3), interval: 3))
        XCTAssertTrue(q.isStale(now: q.time.addingTimeInterval(91), interval: 3))
        XCTAssertTrue(q.isStale(now: q.time.addingTimeInterval(-61), interval: 3))
        XCTAssertFalse(q.isStale(now: q.time.addingTimeInterval(300), interval: 300))
    }
    func testRejectsInvalidQuotes() {
        XCTAssertThrowsError(try Quote.parse(ticker(id: "BTC-USDT-SWAP"), expected: "KORU-USDT-SWAP"))
        XCTAssertThrowsError(try Quote.parse(ticker("0"), expected: "KORU-USDT-SWAP"))
        XCTAssertThrowsError(try Quote.parse(ticker("NaN"), expected: "KORU-USDT-SWAP"))
        XCTAssertThrowsError(try Quote.parse(ticker("7abc"), expected: "KORU-USDT-SWAP"))
        XCTAssertThrowsError(try Quote.parse(ticker(ts: "-1"), expected: "KORU-USDT-SWAP"))
        XCTAssertThrowsError(try Quote.parse(Data("{\"code\":\"50011\",\"data\":[]}".utf8), expected: "KORU-USDT-SWAP"))
    }
    func testFiltersCatalog() throws {
        let data = Data("{\"code\":\"0\",\"data\":[{\"instId\":\"BTC-USDT-SWAP\",\"instType\":\"SWAP\",\"state\":\"live\"},{\"instId\":\"ETH-USDT-SWAP\",\"instType\":\"SWAP\",\"state\":\"suspend\"}]}".utf8)
        let markets = try PublicAPI.catalog(data)
        XCTAssertEqual(markets.count, 1); XCTAssertEqual(markets.first?.coin, "BTC")
    }
}
