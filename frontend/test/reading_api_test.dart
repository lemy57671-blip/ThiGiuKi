import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:toeic_reading/services/reading_api.dart';
import 'package:toeic_reading/services/session_store.dart';

void main() {
  test(
    'Browser session renews the same visitor and preserves its active attempt',
    () async {
      SharedPreferences.setMockInitialValues({
        'reading.jwt': 'legacy-token',
        'reading.activeAttempt': 'legacy-attempt',
      });
      final store = SessionStore(await SharedPreferences.getInstance());
      var calls = 0;
      final api = ReadingApi(
        client: MockClient((request) async {
          expect(request.url.path, '/api/ReadingAccess/Student');
          expect(jsonDecode(request.body), {
            'visitorKey': calls++ == 0 ? null : 'stable-browser-key',
          });
          expect(request.body.contains('password'), isFalse);
          return http.Response(
            jsonEncode({
              'success': true,
              'result': {
                'accessToken': 'token-$calls',
                'visitorKey': 'stable-browser-key',
                'expireInSeconds': 3600,
                'tenantId': 1,
              },
            }),
            200,
          );
        }),
      );
      await store.openSession(api);
      expect(store.activeAttempt, isNull);
      expect(store.hasSession, isTrue);
      await store.setActive('9007199254740993');
      final start = await store.requestId(1);
      await store.openSession(api);
      expect(store.activeAttempt, '9007199254740993');
      expect(await store.requestId(1), start);
      expect(store.visitorKey, 'stable-browser-key');
      expect(store.token, 'token-2');
    },
  );
  test(
    'Concurrent expired requests share one automatic session renewal',
    () async {
      var renewals = 0;
      final api = ReadingApi(
        client: MockClient((request) async {
          if (request.headers['authorization'] != 'Bearer renewed') {
            return http.Response('{"success":false}', 401);
          }
          return http.Response('{"success":true,"result":[]}', 200);
        }),
      )..token = 'expired';
      api.onSessionExpired = () async {
        renewals++;
        await Future<void>.delayed(const Duration(milliseconds: 10));
        api.token = 'renewed';
      };
      await Future.wait([api.exams(), api.attempts()]);
      expect(renewals, 1);
    },
  );
  test(
    'Saves stable option ID and expected rowversion in the ABP contract',
    () async {
      final api = ReadingApi(
        client: MockClient((request) async {
          expect(request.headers['authorization'], 'Bearer test-session');
          expect(request.headers['abp-tenantid'], '1');
          expect(jsonDecode(request.body), {
            'attemptId': '9007199254740993',
            'questionId': 101,
            'selectedOptionId': 405,
            'expectedRowVersion': 'AQIDBAUGBwg=',
          });
          return http.Response(
            jsonEncode({
              'success': true,
              'result': {
                'questionId': 101,
                'selectedOptionId': 405,
                'rowVersion': 'AgIDBAUGBwg=',
              },
            }),
            200,
          );
        }),
      )..token = 'test-session';
      final saved = await api.save(
        '9007199254740993',
        101,
        405,
        'AQIDBAUGBwg=',
      );
      expect(saved.optionId, 405);
      expect(saved.rowVersion, 'AgIDBAUGBwg=');
    },
  );
  test(
    'Null cancellation stays null and HTTP failure never returns mock answers',
    () async {
      final api = ReadingApi(
        client: MockClient((request) async {
          expect(jsonDecode(request.body)['selectedOptionId'], isNull);
          return http.Response(
            '{"success":false,"error":{"message":"Attempt expired"}}',
            400,
          );
        }),
      );
      await expectLater(
        api.save('1', 101, null, 'AQIDBAUGBwg='),
        throwsA(
          isA<ReadingApiError>().having(
            (e) => e.message,
            'message',
            'Attempt expired',
          ),
        ),
      );
    },
  );
  test('Rejects malformed server JSON and unauthorized responses', () async {
    final invalid = ReadingApi(
      client: MockClient((_) async => http.Response('<html>Error</html>', 500)),
    );
    await expectLater(invalid.exams(), throwsA(isA<ReadingApiError>()));
    final denied = ReadingApi(
      client: MockClient((_) async => http.Response('{"success":false}', 401)),
    );
    await expectLater(
      denied.exams(),
      throwsA(isA<ReadingApiError>().having((e) => e.status, 'status', 401)),
    );
  });
  test(
    'Retry uses the same start UUID; completion permits a new attempt',
    () async {
      SharedPreferences.setMockInitialValues({});
      final store = SessionStore(await SharedPreferences.getInstance());
      final first = await store.requestId(1);
      expect(await store.requestId(1), first);
      await store.clearStart(1);
      expect(await store.requestId(1), isNot(first));
      await store.setActive('9007199254740993');
      expect(
        SessionStore(await SharedPreferences.getInstance()).activeAttempt,
        '9007199254740993',
      );
    },
  );
}
