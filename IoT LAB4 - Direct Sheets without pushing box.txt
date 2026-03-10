// https://script.google.com/macros/s/PUT-YOUR-ID-HERE/exec?IDtag=test&TimeStamp=123&TempC=22.5&Humid=50

function doGet(e) {
  var result = 'Ok';   // only change this if something really went wrong

  if (!e || !e.parameter || Object.keys(e.parameter).length === 0) {
    return ContentService.createTextOutput('No Parameters');
  }

  var ss = SpreadsheetApp.openById('1xw6HalNW6f1cWIh23Eg-DOj7-4dlV8gnMPkE708lSLw');
  var sheet = ss.getActiveSheet();

  var newRow = sheet.getLastRow() + 1;
  var rowData = [];
  rowData[0] = new Date();

  for (var param in e.parameter) {
    var value = String(e.parameter[param]);
    switch (param) {
      case 'IDtag':
        rowData[1] = value;
        break;
      case 'TimeStamp':
        rowData[2] = value;
        break;
      case 'TempC':
        rowData[3] = value;
        break;
      case 'Humid':
        rowData[4] = value;
        break;
      default:
        // ignore any extra parameter like `lib`, `d`, etc.
        Logger.log('Ignoring unknown parameter: ' + param);
    }
  }

  sheet.getRange(newRow, 1, 1, rowData.length).setValues([rowData]);

  return ContentService.createTextOutput(result);
}
