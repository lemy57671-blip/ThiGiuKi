import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:toeic_reading/main.dart';
import 'package:toeic_reading/services/reading_api.dart';
import 'package:toeic_reading/services/session_store.dart';

void main() {
  testWidgets('Opens the library without any login or registration form', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(390, 844);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    SharedPreferences.setMockInitialValues({});
    var sessions = 0;
    final api = ReadingApi(
      client: MockClient((request) async {
        if (request.url.path.endsWith('/Student')) {
          sessions++;
          expect(jsonDecode(request.body), {'visitorKey': null});
          return http.Response(
            jsonEncode({
              'success': true,
              'result': {
                'accessToken': 'session-token',
                'visitorKey': 'browser-key',
                'expireInSeconds': 3600,
                'tenantId': 1,
              },
            }),
            200,
          );
        }
        expect(request.headers['authorization'], 'Bearer session-token');
        return http.Response('{"success":true,"result":[]}', 200);
      }),
    );
    await tester.pumpWidget(
      ToeicReadingApp(
        session: SessionStore(await SharedPreferences.getInstance()),
        api: api,
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('Chưa có đề được kích hoạt'), findsOneWidget);
    expect(find.byType(TextField), findsNothing);
    expect(find.text('Đăng nhập'), findsNothing);
    expect(find.text('Đăng ký'), findsNothing);
    expect(find.byTooltip('Đăng xuất'), findsNothing);
    expect(sessions, 1);
    expect(tester.takeException(), isNull);
  });

  testWidgets(
    'Opening a session can retry a network failure without credentials',
    (tester) async {
      SharedPreferences.setMockInitialValues({});
      var sessions = 0;
      final api = ReadingApi(
        client: MockClient((request) async {
          if (request.url.path.endsWith('/Student')) {
            if (++sessions == 1) {
              return http.Response(
                '{"success":false,"error":{"message":"Máy chủ tạm ngắt"}}',
                503,
                headers: {'content-type': 'application/json; charset=utf-8'},
              );
            }
            return http.Response(
              jsonEncode({
                'success': true,
                'result': {
                  'accessToken': 'session-token',
                  'visitorKey': 'browser-key',
                  'expireInSeconds': 3600,
                  'tenantId': 1,
                },
              }),
              200,
            );
          }
          return http.Response('{"success":true,"result":[]}', 200);
        }),
      );
      await tester.pumpWidget(
        ToeicReadingApp(
          session: SessionStore(await SharedPreferences.getInstance()),
          api: api,
        ),
      );
      await tester.pumpAndSettle();
      expect(find.text('Máy chủ tạm ngắt'), findsOneWidget);
      expect(find.byType(TextField), findsNothing);
      await tester.tap(find.text('Thử lại'));
      await tester.pumpAndSettle();
      expect(find.text('Chưa có đề được kích hoạt'), findsOneWidget);
      expect(sessions, 2);
      expect(tester.takeException(), isNull);
    },
  );
}
