import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:toeic_reading/api/abp_api_client.dart';

void main() {
  test('Reads ABP result and respects the configured base path', () async {
    final api = AbpApiClient(
      baseUrl: 'https://example.test/toeic',
      client: MockClient((request) async {
        expect(
          request.url.toString(),
          'https://example.test/toeic/api/services/app/Session/GetCurrentLoginInformations',
        );
        expect(request.headers['Accept'], 'application/json');
        return http.Response(
          '{"result":{"application":{"version":"10.2.0"}},"success":true,"error":null,"__abp":true}',
          200,
        );
      }),
    );
    addTearDown(api.close);
    expect((await api.getLoginInformation()).version, '10.2.0');
  });

  test('Rejects an ABP failure even if HTTP status is 200', () async {
    final api = AbpApiClient(
      client: MockClient(
        (_) async => http.Response(
          '{"success":false,"error":{"message":"Denied"}}',
          200,
        ),
      ),
    );
    addTearDown(api.close);
    await expectLater(
      api.getLoginInformation(),
      throwsA(
        isA<ApiException>().having((e) => e.message, 'message', 'Denied'),
      ),
    );
  });

  test('Rejects HTML and a response missing the ABP result', () async {
    for (final body in ['<html>Login</html>', '{"success":true}']) {
      final api = AbpApiClient(
        client: MockClient((_) async => http.Response(body, 200)),
      );
      try {
        await expectLater(
          api.getLoginInformation(),
          throwsA(isA<ApiException>()),
        );
      } finally {
        api.close();
      }
    }
  });
}
