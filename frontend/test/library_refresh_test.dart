import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:toeic_reading/screens/library_screen.dart';
import 'package:toeic_reading/services/reading_api.dart';
import 'package:toeic_reading/services/session_store.dart';

void main() {
  testWidgets(
    'Returning to the library reloads an exam archived by the teacher',
    (tester) async {
      tester.view.physicalSize = const Size(390, 844);
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);
      SharedPreferences.setMockInitialValues({});
      var archived = false;
      var listRequests = 0;
      final api = ReadingApi(
        client: MockClient((request) async {
          final exams = request.url.path.endsWith('/Exams');
          if (exams) listRequests++;
          return http.Response(
            jsonEncode({
              'success': true,
              'result': exams && !archived
                  ? [
                      {
                        'id': 7,
                        'title': 'Đề sẽ được lưu trữ',
                        'sourceId': 'RC06',
                        'version': 'test',
                        'status': 'PUBLISHED',
                        'durationMinutes': 75,
                        'questionCount': 100,
                      },
                    ]
                  : [],
            }),
            200,
            headers: {'content-type': 'application/json; charset=utf-8'},
          );
        }),
      );
      addTearDown(api.close);
      await tester.pumpWidget(
        MaterialApp(
          home: LibraryScreen(
            api: api,
            session: SessionStore(await SharedPreferences.getInstance()),
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(find.text('Đề sẽ được lưu trữ'), findsOneWidget);
      expect(listRequests, 1);
      archived = true;
      tester.binding.handleAppLifecycleStateChanged(AppLifecycleState.inactive);
      tester.binding.handleAppLifecycleStateChanged(AppLifecycleState.resumed);
      await tester.pumpAndSettle();
      expect(listRequests, 2);
      expect(find.text('Đề sẽ được lưu trữ'), findsNothing);
      expect(find.text('Chưa có đề được kích hoạt'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );
}
